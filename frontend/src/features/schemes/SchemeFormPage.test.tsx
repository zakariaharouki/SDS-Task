import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderApp } from '../../test/renderApp'
import { makeScheme } from '../../test/schemes'
import type { Scheme } from '../../types/scheme'

const NEEDS_HOME =
  'A scheme in Planning, On site or Complete must have at least 1 home.'

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status })

const validationProblem = (errors: Record<string, string[]>) => ({
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
  title: 'One or more validation errors occurred.',
  status: 400,
  errors,
})

const summaryOf = (schemes: Scheme[]) => ({
  schemeCount: schemes.length,
  totalHomes: 0,
  affordableHomes: 0,
  affordablePercent: null,
  totalValue: 0,
  byStatus: [],
})

const fetchMock = vi.fn<typeof fetch>()
let saved: Scheme[]
let answerPost: () => Response | Promise<Response>

const postsMade = () =>
  fetchMock.mock.calls.filter(([, init]) => init?.method === 'POST')

const postedBody = () => JSON.parse(String(postsMade().at(-1)?.[1]?.body))

const requestsTo = (path: string) =>
  fetchMock.mock.calls
    .filter(([, init]) => !init)
    .map(([input]) => new URL(String(input)))
    .filter((url) => url.pathname === path)
    .map((url) => url.search)

const type = (label: string, value: string) => {
  const input = screen.getByLabelText(label)
  fireEvent.change(input, { target: { value } })
  fireEvent.blur(input)
}

const save = () =>
  fireEvent.click(screen.getByRole('button', { name: 'Save scheme' }))

const fillValidForm = () => {
  type('Name', 'Riverside')
  type('Site address', 'Leeds')
}

const errorFor = (label: string) => {
  const input = screen.getByLabelText(label)
  const id = input.getAttribute('aria-describedby')
  return id ? document.getElementById(id)?.textContent : undefined
}

beforeEach(() => {
  saved = []
  answerPost = () => json(makeScheme({ name: 'Riverside' }), 201)
  fetchMock.mockReset()
  fetchMock.mockImplementation(async (input, init) => {
    if (init?.method === 'POST') {
      const response = await answerPost()
      if (response.ok) {
        saved = [makeScheme({ name: 'Riverside' })]
      }
      return response
    }

    const { pathname } = new URL(String(input))

    if (pathname.endsWith('/summary')) {
      return json(summaryOf(saved))
    }

    return pathname === '/api/schemes'
      ? json(saved)
      : json(saved.find((scheme) => pathname.endsWith(scheme.id)))
  })
  vi.stubGlobal('fetch', fetchMock)
  vi.stubEnv('VITE_API_BASE_URL', 'https://api.test')
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
})

describe('a new form', () => {
  it('starts at Appraisal with every tenure at 0', () => {
    renderApp('/schemes/new')

    expect(screen.getByRole('heading', { name: 'Add scheme' })).toBeTruthy()
    expect((screen.getByLabelText('Status') as HTMLSelectElement).value).toBe(
      'Appraisal',
    )
    for (const tenure of [
      'Social rent',
      'Affordable rent',
      'Shared ownership',
      'Market sale',
    ]) {
      expect(
        (screen.getByLabelText(`${tenure} homes`) as HTMLInputElement).value,
      ).toBe('0')
      expect(
        (screen.getByLabelText(`${tenure} average value`) as HTMLInputElement)
          .value,
      ).toBe('0')
    }
  })

  it('offers every status with its friendly label', () => {
    renderApp('/schemes/new')

    expect(
      within(screen.getByLabelText('Status'))
        .getAllByRole('option')
        .map((option) => option.textContent),
    ).toEqual(['Appraisal', 'Planning', 'On site', 'Complete'])
  })

  it('shows nothing wrong before anything has been typed', () => {
    renderApp('/schemes/new')

    expect(document.querySelector('.field-error')).toBeNull()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('can save an Appraisal scheme with no mix yet', async () => {
    renderApp('/schemes/new')
    fillValidForm()

    save()

    await waitFor(() => expect(postsMade()).toHaveLength(1))
    expect(postedBody().status).toBe('Appraisal')
    expect(postedBody().tenureMix.socialRent).toEqual({
      homes: 0,
      averageValue: 0,
    })
  })
})

describe('client rules', () => {
  it('shows the errors on submit, links them to the inputs and sends nothing', async () => {
    renderApp('/schemes/new')

    save()

    await waitFor(() => expect(errorFor('Name')).toBe('Name is required.'))
    expect(errorFor('Site address')).toBe('Site address is required.')
    expect(screen.getByLabelText('Name').getAttribute('aria-invalid')).toBe('true')
    expect(document.activeElement).toBe(screen.getByLabelText('Name'))
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('treats a name of only spaces as missing', async () => {
    renderApp('/schemes/new')

    type('Name', '    ')

    await waitFor(() => expect(errorFor('Name')).toBe('Name is required.'))
  })

  it('shows an error when leaving a field, and clears it once fixed', async () => {
    renderApp('/schemes/new')

    type('Social rent homes', '2.5')
    await waitFor(() =>
      expect(errorFor('Social rent homes')).toBe('Homes must be a whole number.'),
    )

    type('Social rent homes', '2')
    await waitFor(() => expect(errorFor('Social rent homes')).toBeUndefined())
  })

  it('rechecks a visited average value when the homes on that line change', async () => {
    renderApp('/schemes/new')
    type('Market sale average value', '0')

    type('Market sale homes', '5')

    await waitFor(() =>
      expect(errorFor('Market sale average value')).toBe(
        'Average value must be greater than £0 when there are homes.',
      ),
    )

    type('Market sale homes', '0')

    await waitFor(() =>
      expect(errorFor('Market sale average value')).toBeUndefined(),
    )
  })

  it('does not flag the average value before it has been reached', async () => {
    renderApp('/schemes/new')

    type('Market sale homes', '5')
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(errorFor('Market sale average value')).toBeUndefined()
  })

  it('clears an average value error while typing, so the Save button does not move when clicked', async () => {
    renderApp('/schemes/new')
    type('Market sale homes', '5')
    type('Market sale average value', '0')
    await waitFor(() =>
      expect(errorFor('Market sale average value')).toBe(
        'Average value must be greater than £0 when there are homes.',
      ),
    )

    fireEvent.change(screen.getByLabelText('Market sale average value'), {
      target: { value: '100' },
    })

    await waitFor(() =>
      expect(errorFor('Market sale average value')).toBeUndefined(),
    )
  })

  it('keeps focus in the field when the mouse goes down on Save, so no error can move the button', () => {
    renderApp('/schemes/new')

    const notPrevented = fireEvent.mouseDown(
      screen.getByRole('button', { name: 'Save scheme' }),
    )

    expect(notPrevented).toBe(false)
  })

  it('saves on the first click after filling homes and then a value', async () => {
    renderApp('/schemes/new')
    fillValidForm()
    fireEvent.change(screen.getByLabelText('Market sale homes'), {
      target: { value: '5' },
    })
    fireEvent.blur(screen.getByLabelText('Market sale homes'))
    fireEvent.change(screen.getByLabelText('Market sale average value'), {
      target: { value: '0.07' },
    })

    expect(document.querySelector('.field-error')).toBeNull()
    save()

    await screen.findByText('Scheme saved.')
    expect(postsMade()).toHaveLength(1)
  })

  it('does not send a form with a bad number', async () => {
    renderApp('/schemes/new')
    fillValidForm()
    type('Market sale average value', '1.234')

    save()

    await waitFor(() =>
      expect(errorFor('Market sale average value')).toBe(
        'Average value can have at most 2 decimal places.',
      ),
    )
    expect(postsMade()).toHaveLength(0)
  })
})

describe('0 homes past Appraisal', () => {
  it('warns as soon as the status changes, before submit', async () => {
    renderApp('/schemes/new')

    fireEvent.change(screen.getByLabelText('Status'), {
      target: { value: 'Planning' },
    })

    expect((await screen.findByRole('alert')).textContent).toContain(NEEDS_HOME)
  })

  it('goes away when a home is added or the status goes back', async () => {
    renderApp('/schemes/new')
    fireEvent.change(screen.getByLabelText('Status'), {
      target: { value: 'OnSite' },
    })
    await screen.findByRole('alert')

    fireEvent.change(screen.getByLabelText('Status'), {
      target: { value: 'Appraisal' },
    })
    await waitFor(() => expect(screen.queryByRole('alert')).toBeNull())

    fireEvent.change(screen.getByLabelText('Status'), {
      target: { value: 'Complete' },
    })
    await screen.findByRole('alert')

    type('Market sale homes', '1')
    await waitFor(() => expect(screen.queryByRole('alert')).toBeNull())
  })

  it('stops the save and moves to the first homes field', async () => {
    renderApp('/schemes/new')
    fillValidForm()
    fireEvent.change(screen.getByLabelText('Status'), {
      target: { value: 'Planning' },
    })

    save()

    await waitFor(() =>
      expect(document.activeElement).toBe(
        screen.getByLabelText('Social rent homes'),
      ),
    )
    expect(postsMade()).toHaveLength(0)
  })
})

describe('preview', () => {
  it('is labelled as a preview and updates while typing', async () => {
    renderApp('/schemes/new')
    const row = () =>
      within(screen.getByRole('row', { name: /^Social rent/ }))

    expect(
      screen.getByRole('columnheader', { name: 'Line value (preview)' }),
    ).toBeTruthy()
    expect(row().getByText('£0.00')).toBeTruthy()

    fireEvent.change(screen.getByLabelText('Social rent homes'), {
      target: { value: '3' },
    })
    fireEvent.change(screen.getByLabelText('Social rent average value'), {
      target: { value: '1234567.89' },
    })
    fireEvent.change(screen.getByLabelText('Market sale homes'), {
      target: { value: '2' },
    })

    await waitFor(() => expect(row().getByText('£3,703,703.67')).toBeTruthy())
    const preview = document.querySelector('.form-preview')?.textContent

    expect(preview).toContain('Preview:')
    expect(preview).toContain('5 homes')
    expect(preview).toContain('3 affordable')
    expect(preview).toContain('£3,703,703.67')
    expect(preview).toContain('The saved figures come from the server.')
  })

  it('shows a dash instead of a wrong figure while a number is invalid', async () => {
    renderApp('/schemes/new')

    fireEvent.change(screen.getByLabelText('Social rent homes'), {
      target: { value: 'abc' },
    })

    await waitFor(() =>
      expect(
        within(screen.getByRole('row', { name: /^Social rent/ })).getByText('—'),
      ).toBeTruthy(),
    )
    expect(document.querySelector('.form-preview')?.textContent).toContain(
      '— homes',
    )
  })
})

describe('saving', () => {
  it('posts the trimmed values as numbers, with nothing extra', async () => {
    renderApp('/schemes/new')
    type('Name', '  Riverside  ')
    type('Site address', ' Leeds ')
    fireEvent.change(screen.getByLabelText('Status'), {
      target: { value: 'OnSite' },
    })
    type('Market sale homes', '10')
    type('Market sale average value', '250000.50')

    save()

    await waitFor(() => expect(postsMade()).toHaveLength(1))
    const [input, init] = postsMade()[0]

    expect(String(input)).toBe('https://api.test/api/schemes')
    expect(new Headers(init?.headers).get('Content-Type')).toBe(
      'application/json',
    )
    expect(postedBody()).toEqual({
      name: 'Riverside',
      siteAddress: 'Leeds',
      status: 'OnSite',
      tenureMix: {
        socialRent: { homes: 0, averageValue: 0 },
        affordableRent: { homes: 0, averageValue: 0 },
        sharedOwnership: { homes: 0, averageValue: 0 },
        marketSale: { homes: 10, averageValue: 250000.5 },
      },
    })
  })

  it('returns to the list with a message and the new scheme showing', async () => {
    renderApp('/schemes/new')
    fillValidForm()

    save()

    expect((await screen.findByText('Scheme saved.')).getAttribute('role')).toBe(
      'status',
    )
    expect(screen.getByTestId('address').textContent).toBe('/')
    expect(await screen.findByRole('rowheader', { name: 'Riverside' })).toBeTruthy()
  })

  it('refreshes data the list had already loaded', async () => {
    renderApp('/')
    await screen.findByText('No schemes yet. Add your first one.')

    fireEvent.click(screen.getByRole('link', { name: 'Add scheme' }))
    fillValidForm()
    save()

    expect(await screen.findByRole('rowheader', { name: 'Riverside' })).toBeTruthy()
    expect(requestsTo('/api/schemes').length).toBeGreaterThan(1)
    expect(requestsTo('/api/schemes/summary').length).toBeGreaterThan(1)
  })

  it('marks every saved list and summary as out of date', async () => {
    const queryClient = renderApp('/schemes/new')
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries')
    fillValidForm()

    save()

    await screen.findByText('Scheme saved.')
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['schemes'] })
  })

  it('does not reopen the saved form with the back button', async () => {
    renderApp('/')
    fireEvent.click(await screen.findByRole('link', { name: 'Add scheme' }))
    fillValidForm()
    save()
    await screen.findByText('Scheme saved.')

    fireEvent.click(screen.getByRole('button', { name: 'Browser back' }))

    await waitFor(() =>
      expect(screen.getByTestId('address').textContent).toBe('/'),
    )
    expect(screen.queryByRole('heading', { name: 'Add scheme' })).toBeNull()
  })

  it('forgets the saved message once shown, so a reload does not repeat it', async () => {
    renderApp('/schemes/new')
    fillValidForm()

    save()

    await screen.findByText('Scheme saved.')
    await waitFor(() =>
      expect(screen.getByTestId('address').getAttribute('data-state')).toBe(
        'null',
      ),
    )
    expect(screen.getByText('Scheme saved.')).toBeTruthy()
  })

  it('keeps the filter and sort when returning after a save', async () => {
    renderApp('/?status=Planning&sortBy=name')
    fireEvent.click(await screen.findByRole('link', { name: 'Add scheme' }))
    fillValidForm()

    save()

    await screen.findByText('Scheme saved.')
    expect(screen.getByTestId('address').textContent).toBe(
      '/?status=Planning&sortBy=name',
    )
    await waitFor(() =>
      expect(requestsTo('/api/schemes').at(-1)).toBe(
        '?status=Planning&sortBy=name',
      ),
    )
  })

  it('shows Saving and blocks a second save while waiting', async () => {
    let finish: (response: Response) => void = () => {}
    answerPost = () =>
      new Promise((resolve) => {
        finish = resolve
      })
    renderApp('/schemes/new')
    fillValidForm()

    save()

    expect((await screen.findByRole('status')).textContent).toBe('Saving…')
    const button = screen.getByRole('button', {
      name: 'Save scheme',
    }) as HTMLButtonElement
    expect(button.disabled).toBe(true)

    fireEvent.click(button)
    fireEvent.submit(button.closest('form') as HTMLFormElement)
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(postsMade()).toHaveLength(1)

    finish(json(makeScheme(), 201))
    await screen.findByText('Scheme saved.')
  })
})

describe('the back link', () => {
  it('keeps the filter and sort, and shows no saved message', async () => {
    renderApp('/?status=OnSite&sortDir=desc')
    fireEvent.click(await screen.findByRole('link', { name: 'Add scheme' }))

    fireEvent.click(screen.getByRole('link', { name: 'Back to schemes' }))

    await screen.findByRole('heading', { name: 'Scheme Pipeline Tracker' })
    expect(screen.getByTestId('address').textContent).toBe(
      '/?status=OnSite&sortDir=desc',
    )
    expect(screen.queryByText('Scheme saved.')).toBeNull()
  })

  it('goes to the plain list from a direct link', () => {
    renderApp('/schemes/new')

    expect(
      screen.getByRole('link', { name: 'Back to schemes' }).getAttribute('href'),
    ).toBe('/')
  })

  it('also keeps the filter from the edit page', async () => {
    saved = [makeScheme({ status: 'Planning' })]
    renderApp('/?status=Planning')
    fireEvent.click(
      await screen.findByRole('button', { name: /^Edit The Old Depot/ }),
    )
    await screen.findByRole('heading', { name: 'Edit scheme' })

    expect(
      screen.getByRole('link', { name: 'Back to schemes' }).getAttribute('href'),
    ).toBe('/?status=Planning')
  })
})

describe('errors from the server', () => {
  it('shows a 400 on the matching fields, including inside the tenure table', async () => {
    answerPost = () =>
      json(
        validationProblem({
          name: ['Name is not allowed.'],
          'tenureMix.marketSale.averageValue': ['Value rejected by the server.'],
          'tenureMix.socialRent.homes': ['Homes rejected by the server.'],
        }),
        400,
      )
    renderApp('/schemes/new')
    fillValidForm()

    save()

    await waitFor(() => expect(errorFor('Name')).toBe('Name is not allowed.'))
    expect(errorFor('Market sale average value')).toBe(
      'Value rejected by the server.',
    )
    expect(errorFor('Social rent homes')).toBe('Homes rejected by the server.')
    expect(screen.getByRole('heading', { name: 'Add scheme' })).toBeTruthy()
    expect(document.activeElement).toBe(screen.getByLabelText('Name'))
  })

  it('shows the tenureMix error from the server, which belongs to no single input', async () => {
    answerPost = () =>
      json(validationProblem({ tenureMix: ['The mix was rejected.'] }), 400)
    renderApp('/schemes/new')
    fillValidForm()

    save()

    expect((await screen.findByRole('alert')).textContent).toContain(
      'The mix was rejected.',
    )
  })

  it('shows errors for fields not on the form, with a way to try again', async () => {
    answerPost = () =>
      json(validationProblem({ body: ['The request body is not valid JSON.'] }), 400)
    renderApp('/schemes/new')
    fillValidForm()

    save()

    expect((await screen.findByRole('alert')).textContent).toContain(
      'The request body is not valid JSON.',
    )
    expect(screen.getByRole('button', { name: 'Try again' })).toBeTruthy()
  })

  it('shows a duplicate 409 on both name and address', async () => {
    const message = 'A scheme with this name already exists at this address.'
    answerPost = () =>
      json(
        {
          ...validationProblem({ name: [message], siteAddress: [message] }),
          title: 'This scheme already exists.',
          status: 409,
        },
        409,
      )
    renderApp('/schemes/new')
    fillValidForm()

    save()

    await waitFor(() => expect(errorFor('Name')).toBe(message))
    expect(errorFor('Site address')).toBe(message)
    expect(screen.queryByText('Scheme saved.')).toBeNull()
  })

  it('keeps a server error when a field only loses focus, because its value is unchanged', async () => {
    const message = 'A scheme with this name already exists at this address.'
    answerPost = () =>
      json(
        { ...validationProblem({ name: [message], siteAddress: [message] }), status: 409 },
        409,
      )
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await waitFor(() => expect(errorFor('Site address')).toBe(message))

    fireEvent.blur(screen.getByLabelText('Site address'))
    fireEvent.blur(screen.getByLabelText('Name'))
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(errorFor('Name')).toBe(message)
    expect(errorFor('Site address')).toBe(message)
  })

  it('clears the duplicate error from the name when the address is changed', async () => {
    const message = 'A scheme with this name already exists at this address.'
    answerPost = () =>
      json(
        { ...validationProblem({ name: [message], siteAddress: [message] }), status: 409 },
        409,
      )
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await waitFor(() => expect(errorFor('Name')).toBe(message))

    fireEvent.change(screen.getByLabelText('Site address'), {
      target: { value: 'York' },
    })

    await waitFor(() => expect(errorFor('Name')).toBeUndefined())
    expect(errorFor('Site address')).toBeUndefined()
  })

  it('removes old field errors as soon as a new save starts', async () => {
    const message = 'A scheme with this name already exists at this address.'
    answerPost = () =>
      json(
        { ...validationProblem({ name: [message], siteAddress: [message] }), status: 409 },
        409,
      )
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await waitFor(() => expect(errorFor('Name')).toBe(message))

    answerPost = () => new Promise(() => {})
    save()

    await screen.findByText('Saving…')
    expect(errorFor('Name')).toBeUndefined()
    expect(errorFor('Site address')).toBeUndefined()
  })

  it('lets the same values be sent again after a server error', async () => {
    const message = 'A scheme with this name already exists at this address.'
    answerPost = () =>
      json(
        { ...validationProblem({ name: [message], siteAddress: [message] }), status: 409 },
        409,
      )
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await waitFor(() => expect(errorFor('Name')).toBe(message))

    answerPost = () => json(makeScheme({ name: 'Riverside' }), 201)
    save()

    await screen.findByText('Scheme saved.')
    expect(postsMade()).toHaveLength(2)
  })

  it('clears a server error on a tenure line when that line changes, and leaves the others', async () => {
    answerPost = () =>
      json(
        validationProblem({
          'tenureMix.socialRent.averageValue': ['Social value rejected.'],
          'tenureMix.marketSale.homes': ['Market homes rejected.'],
        }),
        400,
      )
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await waitFor(() =>
      expect(errorFor('Social rent average value')).toBe('Social value rejected.'),
    )

    fireEvent.change(screen.getByLabelText('Market sale homes'), {
      target: { value: '1' },
    })

    await waitFor(() => expect(errorFor('Market sale homes')).toBeUndefined())
    expect(errorFor('Social rent average value')).toBe('Social value rejected.')

    fireEvent.change(screen.getByLabelText('Social rent average value'), {
      target: { value: '5' },
    })

    await waitFor(() =>
      expect(errorFor('Social rent average value')).toBeUndefined(),
    )
  })

  it('shows a server error on the status field, and clears it when the status changes', async () => {
    answerPost = () =>
      json(validationProblem({ status: ['Status rejected.'] }), 400)
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await waitFor(() => expect(errorFor('Status')).toBe('Status rejected.'))

    fireEvent.change(screen.getByLabelText('Status'), {
      target: { value: 'Appraisal' },
    })

    await waitFor(() => expect(errorFor('Status')).toBeUndefined())
  })

  it('clears the duplicate error from the other field when either is changed', async () => {
    const message = 'A scheme with this name already exists at this address.'
    answerPost = () =>
      json(
        { ...validationProblem({ name: [message], siteAddress: [message] }), status: 409 },
        409,
      )
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await waitFor(() => expect(errorFor('Site address')).toBe(message))

    type('Name', 'Riverside Phase 2')

    await waitFor(() => expect(errorFor('Site address')).toBeUndefined())
    await waitFor(() => expect(errorFor('Name')).toBeUndefined())
  })

  it('keeps everything typed on a 500, and Try again sends it again', async () => {
    answerPost = () =>
      json(
        {
          type: 't',
          title: 'An error occurred while processing your request.',
          status: 500,
        },
        500,
      )
    renderApp('/schemes/new')
    fillValidForm()
    type('Market sale homes', '4')
    type('Market sale average value', '100')

    save()

    expect((await screen.findByRole('alert')).textContent).toContain(
      'An error occurred while processing your request.',
    )
    expect((screen.getByLabelText('Name') as HTMLInputElement).value).toBe(
      'Riverside',
    )
    expect(
      (screen.getByLabelText('Market sale homes') as HTMLInputElement).value,
    ).toBe('4')

    answerPost = () => json(makeScheme({ name: 'Riverside' }), 201)
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await screen.findByText('Scheme saved.')
    expect(postsMade()).toHaveLength(2)
    expect(postedBody().tenureMix.marketSale).toEqual({
      homes: 4,
      averageValue: 100,
    })
  })

  it('gives a friendly message when the server cannot be reached', async () => {
    answerPost = () => Promise.reject(new TypeError('Failed to fetch'))
    renderApp('/schemes/new')
    fillValidForm()

    save()

    const alert = await screen.findByRole('alert')

    expect(alert.textContent).toContain('Could not reach the server.')
    expect(alert.textContent).not.toContain('Failed to fetch')
  })

  it('clears an old error when saving again', async () => {
    answerPost = () => json({ type: 't', title: 'Down', status: 503 }, 503)
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await screen.findByRole('alert')

    answerPost = () => new Promise(() => {})
    save()

    await screen.findByText('Saving…')
    expect(screen.queryByRole('alert')).toBeNull()
  })
})

describe('the saved message', () => {
  it('does not appear on a normal visit to the list', async () => {
    renderApp('/')

    await screen.findByText('No schemes yet. Add your first one.')

    expect(screen.queryByText('Scheme saved.')).toBeNull()
  })

  it('stays while the filter changes on the same visit', async () => {
    renderApp('/schemes/new')
    fillValidForm()
    save()
    await screen.findByText('Scheme saved.')

    fireEvent.click(await screen.findByRole('checkbox', { name: 'Planning' }))

    await waitFor(() =>
      expect(screen.getByTestId('address').textContent).toBe('/?status=Planning'),
    )
    expect(screen.getByText('Scheme saved.')).toBeTruthy()
  })
})
