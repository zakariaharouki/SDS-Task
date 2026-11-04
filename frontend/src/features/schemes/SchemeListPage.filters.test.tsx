import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderApp } from '../../test/renderApp'
import { makeScheme } from '../../test/schemes'
import type { Scheme } from '../../types/scheme'

const allSchemes = [
  makeScheme({ id: 'a', name: 'Alder', status: 'Planning', totalHomes: 10 }),
  makeScheme({ id: 'b', name: 'Birch', status: 'OnSite', totalHomes: 20 }),
  makeScheme({ id: 'c', name: 'Cedar', status: 'Complete', totalHomes: 40 }),
]

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status })

const summaryOf = (schemes: Scheme[]) => ({
  schemeCount: schemes.length,
  totalHomes: schemes.reduce((total, scheme) => total + scheme.totalHomes, 0),
  affordableHomes: 0,
  affordablePercent: null,
  totalValue: 0,
  byStatus: [],
})

const fetchMock = vi.fn<typeof fetch>()

// Answers like the API: both endpoints follow the status filter.
const answerLikeTheApi = async (input: RequestInfo | URL) => {
  const url = new URL(String(input))
  const statuses = url.searchParams.getAll('status')
  const matching = allSchemes.filter(
    (scheme) => statuses.length === 0 || statuses.includes(scheme.status),
  )

  return json(
    url.pathname.endsWith('/summary') ? summaryOf(matching) : matching,
  )
}

const requestsTo = (path: string) =>
  fetchMock.mock.calls
    .map(([input]) => new URL(String(input)))
    .filter((url) => url.pathname === path)
    .map((url) => url.search)

const lastRequestTo = (path: string) => requestsTo(path).at(-1)

const address = () => screen.getByTestId('address').textContent

const names = () =>
  screen.getAllByRole('rowheader').map((header) => header.textContent)

const cardValue = (label: string) =>
  within(screen.getByRole('region', { name: 'Portfolio summary' }))
    .getByText(label)
    .parentElement?.querySelector('strong')?.textContent

const tick = (name: string) =>
  fireEvent.click(screen.getByRole('checkbox', { name }))

const isTicked = (name: string) =>
  (screen.getByRole('checkbox', { name }) as HTMLInputElement).checked

beforeEach(() => {
  fetchMock.mockReset()
  fetchMock.mockImplementation(answerLikeTheApi)
  vi.stubGlobal('fetch', fetchMock)
  vi.stubEnv('VITE_API_BASE_URL', 'https://api.test')
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
})

describe('filtering', () => {
  it('ticking a status updates the address, both requests and the page', async () => {
    renderApp()
    await screen.findByRole('table')

    tick('On site')

    await waitFor(() => expect(names()).toEqual(['Birch']))
    expect(address()).toBe('/?status=OnSite')
    expect(lastRequestTo('/api/schemes')).toBe('?status=OnSite')
    expect(lastRequestTo('/api/schemes/summary')).toBe('?status=OnSite')
    expect(screen.getByText('Showing 1 of 4 statuses')).toBeTruthy()
  })

  it('the summary cards follow the filter', async () => {
    renderApp()
    await screen.findByRole('table')
    expect(cardValue('Total homes')).toBe('70')

    tick('Planning')

    await waitFor(() => expect(cardValue('Total homes')).toBe('10'))
    expect(cardValue('Schemes')).toBe('1')
  })

  it('several statuses are repeated in pipeline order, whatever order they are ticked', async () => {
    renderApp()
    await screen.findByRole('table')

    tick('Complete')
    await waitFor(() => expect(address()).toBe('/?status=Complete'))
    tick('Planning')

    await waitFor(() => expect(names()).toEqual(['Alder', 'Cedar']))
    expect(address()).toBe('/?status=Planning&status=Complete')
    expect(lastRequestTo('/api/schemes')).toBe(
      '?status=Planning&status=Complete',
    )
    expect(screen.getByText('Showing 2 of 4 statuses')).toBeTruthy()
  })

  it('unticking the last status goes back to the plain address', async () => {
    renderApp('/?status=Planning')
    await screen.findByRole('table')

    tick('Planning')

    await waitFor(() => expect(names()).toHaveLength(3))
    expect(address()).toBe('/')
    expect(lastRequestTo('/api/schemes')).toBe('')
    expect(screen.queryByText(/^Showing/)).toBeNull()
  })

  it('a shared link opens with its filter already applied', async () => {
    renderApp('/?status=Planning&status=OnSite')
    await screen.findByRole('table')

    expect(isTicked('Planning')).toBe(true)
    expect(isTicked('On site')).toBe(true)
    expect(isTicked('Complete')).toBe(false)
    expect(names()).toEqual(['Alder', 'Birch'])
    expect(requestsTo('/api/schemes')).toEqual(['?status=Planning&status=OnSite'])
  })

  it('keeps the current sort when the filter changes', async () => {
    renderApp('/?sortBy=name&sortDir=desc')
    await screen.findByRole('table')

    tick('Planning')

    await waitFor(() =>
      expect(address()).toBe('/?status=Planning&sortBy=name&sortDir=desc'),
    )
  })
})

describe('sorting', () => {
  it('clicking a heading updates the address, the request and the announcement', async () => {
    renderApp()
    await screen.findByRole('table')

    fireEvent.click(screen.getByRole('button', { name: 'Total value' }))

    await waitFor(() => expect(address()).toBe('/?sortBy=totalValue'))
    await waitFor(() =>
      expect(lastRequestTo('/api/schemes')).toBe('?sortBy=totalValue'),
    )
    expect(
      screen
        .getByRole('columnheader', { name: 'Total value' })
        .getAttribute('aria-sort'),
    ).toBe('ascending')
  })

  it('clicking the same heading again reverses it', async () => {
    renderApp('/?sortBy=name')
    await screen.findByRole('table')

    fireEvent.click(screen.getByRole('button', { name: 'Name' }))

    await waitFor(() => expect(address()).toBe('/?sortBy=name&sortDir=desc'))
    await waitFor(() =>
      expect(lastRequestTo('/api/schemes')).toBe('?sortBy=name&sortDir=desc'),
    )
    expect(
      screen.getByRole('columnheader', { name: 'Name' }).getAttribute('aria-sort'),
    ).toBe('descending')
  })

  it('a shared link opens with its sort already shown on the right column', async () => {
    renderApp('/?sortBy=totalHomes&sortDir=desc')
    await screen.findByRole('table')

    expect(
      screen
        .getByRole('columnheader', { name: 'Total homes' })
        .getAttribute('aria-sort'),
    ).toBe('descending')
    expect(requestsTo('/api/schemes')).toEqual(['?sortBy=totalHomes&sortDir=desc'])
  })

  it('sorting does not ask for the summary again, because the totals are the same', async () => {
    renderApp()
    await screen.findByRole('table')

    fireEvent.click(screen.getByRole('button', { name: 'Name' }))

    await waitFor(() => expect(requestsTo('/api/schemes')).toHaveLength(2))
    expect(requestsTo('/api/schemes/summary')).toHaveLength(1)
  })

  it('shows the rows in the order the API returns them', async () => {
    fetchMock.mockImplementation(async (input) =>
      new URL(String(input)).pathname.endsWith('/summary')
        ? json(summaryOf(allSchemes))
        : json([...allSchemes].reverse()),
    )
    renderApp()
    await screen.findByRole('table')

    expect(names()).toEqual(['Cedar', 'Birch', 'Alder'])
  })
})

describe('bad links', () => {
  it.each([
    '/?status=planning',
    '/?status=1',
    '/?status=Done',
    '/?sortBy=price',
    '/?sortBy=updatedAt',
    '/?sortDir=sideways',
    '/?status=planning&sortBy=price&sortDir=sideways',
  ])('%s shows the normal list without sending the bad value', async (route) => {
    renderApp(route)
    await screen.findByRole('table')

    expect(names()).toHaveLength(3)
    expect(screen.queryByRole('alert')).toBeNull()
    expect(requestsTo('/api/schemes')).toEqual([''])
    expect(requestsTo('/api/schemes/summary')).toEqual([''])
  })

  it('keeps the good values from a partly bad link', async () => {
    renderApp('/?status=Planning&status=nope&sortBy=nope')
    await screen.findByRole('table')

    expect(requestsTo('/api/schemes')).toEqual(['?status=Planning'])
  })
})

describe('no matches', () => {
  it('shows its own message, and Clear filter brings back every status', async () => {
    renderApp('/?status=Appraisal&sortBy=name')

    expect(await screen.findByText('No schemes match this filter.')).toBeTruthy()
    expect(screen.queryByRole('table')).toBeNull()
    expect(screen.queryByText(/No schemes yet/)).toBeNull()
    expect(cardValue('Schemes')).toBe('0')

    fireEvent.click(screen.getByRole('button', { name: 'Clear filter' }))

    await waitFor(() => expect(names()).toHaveLength(3))
    expect(address()).toBe('/?sortBy=name')
    expect(isTicked('Appraisal')).toBe(false)
  })

  it('still says there are no schemes yet when nothing is filtered', async () => {
    fetchMock.mockImplementation(async (input) =>
      new URL(String(input)).pathname.endsWith('/summary')
        ? json(summaryOf([]))
        : json([]),
    )
    renderApp()

    expect(
      await screen.findByText('No schemes yet. Add your first one.'),
    ).toBeTruthy()
    expect(screen.queryByText('No schemes match this filter.')).toBeNull()
  })

  it('treats every status ticked as no filter', async () => {
    fetchMock.mockImplementation(async (input) =>
      new URL(String(input)).pathname.endsWith('/summary')
        ? json(summaryOf([]))
        : json([]),
    )
    renderApp(
      '/?status=Appraisal&status=Planning&status=OnSite&status=Complete',
    )

    expect(
      await screen.findByText('No schemes yet. Add your first one.'),
    ).toBeTruthy()
  })
})

describe('while a change is loading or fails', () => {
  it('keeps the current table instead of showing Loading', async () => {
    renderApp()
    await screen.findByRole('table')
    fetchMock.mockImplementation(() => new Promise(() => {}))

    tick('Planning')

    await waitFor(() =>
      expect(
        document.querySelector('.scheme-results')?.getAttribute('aria-busy'),
      ).toBe('true'),
    )
    expect(screen.getByRole('table')).toBeTruthy()
    expect(names()).toHaveLength(3)
    expect(screen.queryByRole('status')).toBeNull()
  })

  it('is not marked busy once everything has loaded', async () => {
    renderApp()
    await screen.findByRole('table')

    expect(
      document.querySelector('.scheme-results')?.getAttribute('aria-busy'),
    ).toBe('false')
  })

  it('shows the error when a new filter fails, keeps the filter, and can retry', async () => {
    renderApp()
    await screen.findByRole('table')
    fetchMock.mockImplementation(async () =>
      json({ type: 't', title: 'Service down', status: 503 }, 503),
    )

    tick('Planning')

    expect((await screen.findByRole('alert')).textContent).toContain(
      'Service down',
    )
    expect(isTicked('Planning')).toBe(true)
    expect(screen.queryByRole('table')).toBeNull()

    fetchMock.mockImplementation(answerLikeTheApi)
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(names()).toEqual(['Alder']))
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('shows the filter while the first load is still running', () => {
    fetchMock.mockImplementation(() => new Promise(() => {}))
    renderApp()

    expect(screen.getByRole('group', { name: 'Filter by status' })).toBeTruthy()
    expect(screen.getByRole('status')).toBeTruthy()
  })
})
