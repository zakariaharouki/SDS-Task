import { focusManager } from '@tanstack/react-query'
import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderApp } from '../../test/renderApp'
import { makeScheme } from '../../test/schemes'
import type { Scheme } from '../../types/scheme'

const CONFLICT_MESSAGE =
  'This scheme was changed by someone else. Reload to see the latest version.'
const DUPLICATE_MESSAGE =
  'A scheme with this name already exists at this address.'

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status })

const problem = (title: string, status: number, extra: object = {}) =>
  json({ type: 't', title, status, ...extra }, status)

const conflictResponse = () =>
  problem('This scheme was changed by someone else.', 409, {
    detail: CONFLICT_MESSAGE,
  })

const fetchMock = vi.fn<typeof fetch>()
let stored: Scheme
let answerGet: () => Response | Promise<Response>
let answerPut: () => Response | Promise<Response>

const requests = (method: string, path: string) =>
  fetchMock.mock.calls.filter(
    ([input, init]) =>
      (init?.method ?? 'GET') === method &&
      new URL(String(input)).pathname === path,
  )

const getsOfScheme = () => requests('GET', `/api/schemes/${stored.id}`)
const putsOfScheme = () => requests('PUT', `/api/schemes/${stored.id}`)

const lastPutBody = () => JSON.parse(String(putsOfScheme().at(-1)?.[1]?.body))

const input = (label: string) => screen.getByLabelText(label) as HTMLInputElement

const type = (label: string, value: string) => {
  fireEvent.change(input(label), { target: { value } })
  fireEvent.blur(input(label))
}

const findForm = () => screen.findByRole('button', { name: 'Save changes' })

const save = () =>
  fireEvent.click(screen.getByRole('button', { name: 'Save changes' }))

const errorFor = (label: string) => {
  const id = input(label).getAttribute('aria-describedby')
  return id ? document.getElementById(id)?.textContent : undefined
}

const editAddress = () => `/schemes/${stored.id}/edit`

beforeEach(() => {
  stored = makeScheme({
    id: 'scheme-7',
    name: 'Riverside',
    siteAddress: 'Leeds',
    status: 'OnSite',
    version: 3,
    tenureMix: {
      socialRent: { homes: 10, averageValue: 150000.5 },
      affordableRent: { homes: 5, averageValue: 180000.25 },
      sharedOwnership: { homes: 5, averageValue: 220000 },
      marketSale: { homes: 30, averageValue: 1234567.89 },
    },
  })
  answerGet = () => json(stored)
  answerPut = () => json({ ...stored, version: stored.version + 1 })
  fetchMock.mockReset()
  fetchMock.mockImplementation(async (url, init) => {
    const { pathname } = new URL(String(url))

    if (init?.method === 'PUT') {
      return answerPut()
    }

    if (pathname.endsWith('/summary')) {
      return json({
        schemeCount: 1,
        totalHomes: 50,
        affordableHomes: 20,
        affordablePercent: 40,
        totalValue: 1,
        byStatus: [],
      })
    }

    return pathname === '/api/schemes' ? json([stored]) : answerGet()
  })
  vi.stubGlobal('fetch', fetchMock)
  vi.stubEnv('VITE_API_BASE_URL', 'https://api.test')
})

afterEach(() => {
  focusManager.setFocused(undefined)
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
})

describe('loading the scheme', () => {
  it('fills the form with the saved values', async () => {
    renderApp(editAddress())

    await findForm()

    expect(screen.getByRole('heading', { name: 'Edit scheme' })).toBeTruthy()
    expect(input('Name').value).toBe('Riverside')
    expect(input('Site address').value).toBe('Leeds')
    expect(input('Status').value).toBe('OnSite')
    expect(input('Social rent homes').value).toBe('10')
    expect(input('Social rent average value').value).toBe('150000.5')
    expect(input('Affordable rent average value').value).toBe('180000.25')
    expect(input('Market sale homes').value).toBe('30')
    expect(input('Market sale average value').value).toBe('1234567.89')
    expect(document.querySelector('.form-preview')?.textContent).toContain(
      '50 homes',
    )
    expect(document.querySelector('.field-error')).toBeNull()
  })

  it('shows loading with a way back while waiting', () => {
    answerGet = () => new Promise(() => {})
    renderApp(editAddress())

    expect(screen.getByRole('status').textContent).toBe('Loading…')
    expect(screen.getByRole('link', { name: 'Back to schemes' })).toBeTruthy()
  })

  it('says the scheme could not be found, with no retry, on a 404', async () => {
    answerGet = () => problem('Not Found', 404)
    renderApp(editAddress())

    expect(
      await screen.findByText('This scheme could not be found.'),
    ).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Try again' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Save changes' })).toBeNull()
    expect(screen.getByRole('link', { name: 'Back to schemes' })).toBeTruthy()
  })

  it('shows an error with a retry when loading fails, and recovers', async () => {
    answerGet = () => problem('Service down', 503)
    renderApp(editAddress())

    expect((await screen.findByRole('alert')).textContent).toContain(
      'Service down',
    )
    expect(screen.getByRole('link', { name: 'Back to schemes' })).toBeTruthy()

    answerGet = () => json(stored)
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await findForm()
    expect(input('Name').value).toBe('Riverside')
  })

  it('does not reload over what is being typed when the window regains focus', async () => {
    renderApp(editAddress())
    await findForm()
    type('Name', 'Riverside Phase 2')

    stored = { ...stored, name: 'Changed elsewhere', version: 9 }
    focusManager.setFocused(false)
    focusManager.setFocused(true)
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(input('Name').value).toBe('Riverside Phase 2')
    expect(getsOfScheme()).toHaveLength(1)
  })

  it('loads the latest values again on the next visit', async () => {
    renderApp('/')
    fireEvent.click(await screen.findByRole('button', { name: /^Edit Riverside/ }))
    await findForm()
    fireEvent.click(screen.getByRole('link', { name: 'Back to schemes' }))
    await screen.findByRole('heading', { name: 'Scheme Pipeline Tracker' })

    stored = { ...stored, name: 'Changed elsewhere', version: 9 }
    fireEvent.click(await screen.findByRole('button', { name: /^Edit / }))

    await waitFor(() => expect(input('Name').value).toBe('Changed elsewhere'))
    expect(getsOfScheme()).toHaveLength(2)
  })
})

describe('saving changes', () => {
  it('puts the whole scheme with the version it was loaded at', async () => {
    renderApp(editAddress())
    await findForm()
    type('Name', '  Riverside Phase 2  ')
    type('Market sale homes', '31')

    save()

    await waitFor(() => expect(putsOfScheme()).toHaveLength(1))
    expect(new Headers(putsOfScheme()[0][1]?.headers).get('Content-Type')).toBe(
      'application/json',
    )
    expect(lastPutBody()).toEqual({
      name: 'Riverside Phase 2',
      siteAddress: 'Leeds',
      status: 'OnSite',
      version: 3,
      tenureMix: {
        socialRent: { homes: 10, averageValue: 150000.5 },
        affordableRent: { homes: 5, averageValue: 180000.25 },
        sharedOwnership: { homes: 5, averageValue: 220000 },
        marketSale: { homes: 31, averageValue: 1234567.89 },
      },
    })
  })

  it('returns to the list with the message and the filter kept, and refreshes it', async () => {
    renderApp('/?status=OnSite&sortBy=name')
    fireEvent.click(await screen.findByRole('button', { name: /^Edit Riverside/ }))
    await findForm()
    const listRequestsBefore = requests('GET', '/api/schemes').length

    save()

    await screen.findByText('Scheme saved.')
    expect(screen.getByTestId('address').textContent).toBe(
      '/?status=OnSite&sortBy=name',
    )
    await waitFor(() =>
      expect(requests('GET', '/api/schemes').length).toBeGreaterThan(
        listRequestsBefore,
      ),
    )
  })

  it('marks the list and summary as out of date', async () => {
    const queryClient = renderApp(editAddress())
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries')
    await findForm()

    save()

    await screen.findByText('Scheme saved.')
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['schemes'] })
  })

  it('applies the same rules as the create form', async () => {
    renderApp(editAddress())
    await findForm()
    type('Social rent homes', '0')
    type('Affordable rent homes', '0')
    type('Shared ownership homes', '0')
    type('Market sale homes', '0')

    expect((await screen.findByRole('alert')).textContent).toContain(
      'must have at least 1 home',
    )

    save()
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(putsOfScheme()).toHaveLength(0)
  })

  it('allows moving a scheme back to Appraisal with no homes', async () => {
    renderApp(editAddress())
    await findForm()
    fireEvent.change(input('Status'), { target: { value: 'Appraisal' } })
    type('Social rent homes', '0')
    type('Affordable rent homes', '0')
    type('Shared ownership homes', '0')
    type('Market sale homes', '0')

    save()

    await waitFor(() => expect(putsOfScheme()).toHaveLength(1))
    expect(lastPutBody().status).toBe('Appraisal')
  })
})

describe('when someone else changed the scheme', () => {
  it('shows the reload prompt, keeps the typing and offers no retry', async () => {
    answerPut = conflictResponse
    renderApp(editAddress())
    await findForm()
    type('Name', 'My edit')

    save()

    expect((await screen.findByRole('alert')).textContent).toContain(
      CONFLICT_MESSAGE,
    )
    expect(input('Name').value).toBe('My edit')
    expect(
      screen.getByRole('button', { name: 'Reload latest version' }),
    ).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Try again' })).toBeNull()
    expect(screen.queryByText('Scheme saved.')).toBeNull()
    expect(document.querySelector('.field-error')).toBeNull()
  })

  it('reloading shows the latest values, and the next save uses the new version', async () => {
    answerPut = conflictResponse
    renderApp(editAddress())
    await findForm()
    type('Name', 'My edit')
    save()
    await screen.findByRole('alert')

    stored = { ...stored, name: 'Their edit', version: 4 }
    fireEvent.click(
      screen.getByRole('button', { name: 'Reload latest version' }),
    )

    await waitFor(() => expect(input('Name').value).toBe('Their edit'))
    await waitFor(() => expect(screen.queryByRole('alert')).toBeNull())

    answerPut = () => json({ ...stored, version: 5 })
    save()

    await screen.findByText('Scheme saved.')
    expect(lastPutBody().version).toBe(4)
  })

  it('reloading replaces the typing even if the version is unchanged', async () => {
    answerPut = conflictResponse
    renderApp(editAddress())
    await findForm()
    type('Name', 'My edit')
    save()
    await screen.findByRole('alert')
    await new Promise((resolve) => setTimeout(resolve, 5))

    fireEvent.click(
      screen.getByRole('button', { name: 'Reload latest version' }),
    )

    await waitFor(() => expect(input('Name').value).toBe('Riverside'))
    await waitFor(() => expect(screen.queryByRole('alert')).toBeNull())
  })

  it('keeps the prompt if the reload fails', async () => {
    answerPut = conflictResponse
    renderApp(editAddress())
    await findForm()
    save()
    await screen.findByRole('alert')

    answerGet = () => problem('Service down', 503)
    fireEvent.click(
      screen.getByRole('button', { name: 'Reload latest version' }),
    )

    await waitFor(() => expect(getsOfScheme()).toHaveLength(2))
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(screen.getByRole('alert').textContent).toContain(CONFLICT_MESSAGE)
  })

  it('removes the prompt as soon as another save starts', async () => {
    answerPut = conflictResponse
    renderApp(editAddress())
    await findForm()
    save()
    await screen.findByRole('alert')

    answerPut = () => new Promise(() => {})
    save()

    await screen.findByText('Saving…')
    expect(screen.queryByRole('alert')).toBeNull()
  })
})

describe('other errors when saving', () => {
  it('shows a duplicate 409 on both fields, not as a version conflict', async () => {
    answerPut = () =>
      problem('This scheme already exists.', 409, {
        errors: { name: [DUPLICATE_MESSAGE], siteAddress: [DUPLICATE_MESSAGE] },
      })
    renderApp(editAddress())
    await findForm()

    save()

    await waitFor(() => expect(errorFor('Name')).toBe(DUPLICATE_MESSAGE))
    expect(errorFor('Site address')).toBe(DUPLICATE_MESSAGE)
    expect(
      screen.queryByRole('button', { name: 'Reload latest version' }),
    ).toBeNull()
  })

  it('shows a 400 on the matching tenure field', async () => {
    answerPut = () =>
      problem('Invalid', 400, {
        errors: { 'tenureMix.socialRent.homes': ['Homes rejected.'] },
      })
    renderApp(editAddress())
    await findForm()

    save()

    await waitFor(() => expect(errorFor('Social rent homes')).toBe('Homes rejected.'))
  })

  it('offers Try again on a 500, and it sends the same version', async () => {
    answerPut = () => problem('Server error', 500)
    renderApp(editAddress())
    await findForm()
    type('Name', 'My edit')

    save()

    expect((await screen.findByRole('alert')).textContent).toContain(
      'Server error',
    )
    expect(input('Name').value).toBe('My edit')

    answerPut = () => json(stored)
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await screen.findByText('Scheme saved.')
    expect(lastPutBody().version).toBe(3)
  })

  it('says the scheme could not be found if it is gone when saving', async () => {
    answerPut = () => problem('Not Found', 404)
    renderApp(editAddress())
    await findForm()

    save()

    expect(
      await screen.findByText('This scheme could not be found.'),
    ).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Save changes' })).toBeNull()
  })
})
