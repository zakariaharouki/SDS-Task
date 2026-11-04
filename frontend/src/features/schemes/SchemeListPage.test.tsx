import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderApp } from '../../test/renderApp'
import { makeScheme } from '../../test/schemes'
import type { SchemeSummary } from '../../types/scheme'

const LIST_URL = 'https://api.test/api/schemes'
const SUMMARY_URL = 'https://api.test/api/schemes/summary'

const summary: SchemeSummary = {
  schemeCount: 1,
  totalHomes: 20,
  affordableHomes: 10,
  affordablePercent: 50,
  totalValue: 2469135.6,
  byStatus: [],
}

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status })

const problem = (title: string, status: number) =>
  json({ type: 'about:blank', title, status }, status)

const fetchMock = vi.fn<typeof fetch>()

// Sets what each endpoint answers. Every call gets a new Response.
const respondWith = (
  list: () => Response | Promise<Response>,
  totals: () => Response | Promise<Response>,
) => {
  fetchMock.mockImplementation(async (url) =>
    url === SUMMARY_URL ? totals() : list(),
  )
}

const callsTo = (url: string) =>
  fetchMock.mock.calls.filter(([calledUrl]) => calledUrl === url).length

const findTryAgain = () => screen.findByRole('button', { name: 'Try again' })

beforeEach(() => {
  fetchMock.mockReset()
  vi.stubGlobal('fetch', fetchMock)
  vi.stubEnv('VITE_API_BASE_URL', 'https://api.test')
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
})

describe('SchemeListPage', () => {
  it('shows a loading message while waiting for the API', async () => {
    respondWith(
      () => new Promise(() => {}),
      () => new Promise(() => {}),
    )
    renderApp()

    expect(screen.getByRole('status').textContent).toBe('Loading…')
    expect(screen.queryByRole('table')).toBeNull()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('keeps showing loading until both requests have finished', async () => {
    respondWith(
      () => json([makeScheme()]),
      () => new Promise(() => {}),
    )
    renderApp()

    await waitFor(() => expect(callsTo(LIST_URL)).toBe(1))
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(screen.getByRole('status')).toBeTruthy()
    expect(screen.queryByRole('table')).toBeNull()
  })

  it('shows the summary and the table from one request to each endpoint', async () => {
    respondWith(
      () => json([makeScheme()]),
      () => json(summary),
    )
    renderApp()

    expect(await screen.findByRole('table')).toBeTruthy()
    expect(screen.getByRole('region', { name: 'Portfolio summary' })).toBeTruthy()
    expect(screen.getByRole('rowheader').textContent).toBe('The Old Depot')
    expect(screen.queryByRole('status')).toBeNull()
    expect(callsTo(LIST_URL)).toBe(1)
    expect(callsTo(SUMMARY_URL)).toBe(1)
  })

  it('shows the empty message and zero totals when there are no schemes', async () => {
    respondWith(
      () => json([]),
      () =>
        json({
          ...summary,
          schemeCount: 0,
          totalHomes: 0,
          affordableHomes: 0,
          affordablePercent: null,
          totalValue: 0,
        }),
    )
    renderApp()

    expect(
      await screen.findByText('No schemes yet. Add your first one.'),
    ).toBeTruthy()
    expect(screen.getByText('n/a')).toBeTruthy()
    expect(screen.getByText('£0.00')).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('shows the API error instead of figures when the list fails', async () => {
    respondWith(
      () => problem('An error occurred while processing your request.', 500),
      () => json(summary),
    )
    renderApp()

    const alert = await screen.findByRole('alert')

    expect(alert.textContent).toContain(
      'An error occurred while processing your request.',
    )
    expect(screen.queryByRole('table')).toBeNull()
    expect(screen.queryByRole('region')).toBeNull()
  })

  it('shows the error when only the summary fails', async () => {
    respondWith(
      () => json([makeScheme()]),
      () => problem('Summary failed', 500),
    )
    renderApp()

    expect((await screen.findByRole('alert')).textContent).toContain(
      'Summary failed',
    )
    expect(screen.queryByRole('table')).toBeNull()
  })

  it('shows a friendly message when the server cannot be reached', async () => {
    respondWith(
      () => Promise.reject(new TypeError('Failed to fetch')),
      () => Promise.reject(new TypeError('Failed to fetch')),
    )
    renderApp()

    const alert = await screen.findByRole('alert')

    expect(alert.textContent).toContain('Could not reach the server.')
    expect(alert.textContent).not.toContain('Failed to fetch')
  })

  it('retries only the failed request, shows loading, then the figures', async () => {
    let listAttempts = 0
    let finishRetry: (response: Response) => void = () => {}
    respondWith(
      () => {
        listAttempts += 1
        return listAttempts === 1
          ? problem('Try later', 503)
          : new Promise((resolve) => {
              finishRetry = resolve
            })
      },
      () => json(summary),
    )
    renderApp()

    fireEvent.click(await findTryAgain())

    expect(await screen.findByRole('status')).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()

    finishRetry(json([makeScheme()]))

    expect(await screen.findByRole('table')).toBeTruthy()
    expect(callsTo(LIST_URL)).toBe(2)
    expect(callsTo(SUMMARY_URL)).toBe(1)
  })

  it('goes back to the error when the retry also fails', async () => {
    respondWith(
      () => problem('Still down', 503),
      () => json(summary),
    )
    renderApp()

    fireEvent.click(await findTryAgain())

    await waitFor(() => expect(callsTo(LIST_URL)).toBe(2))
    expect((await screen.findByRole('alert')).textContent).toContain(
      'Still down',
    )
  })

  it('keeps the figures on screen when a background refresh fails', async () => {
    respondWith(
      () => json([makeScheme()]),
      () => json(summary),
    )
    const queryClient = renderApp()
    await screen.findByRole('table')

    respondWith(
      () => problem('Down', 503),
      () => problem('Down', 503),
    )
    await queryClient.refetchQueries()

    expect(screen.getByRole('table')).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('opens the edit page of the chosen scheme', async () => {
    const leeds = makeScheme({ id: 'leeds-id', siteAddress: 'Leeds' })
    const york = makeScheme({ id: 'york-id', siteAddress: 'York' })
    respondWith(
      () => json([leeds, york]),
      () => json(summary),
    )
    renderApp()

    fireEvent.click(
      await screen.findByRole('button', { name: 'Edit The Old Depot, York' }),
    )

    expect(
      await screen.findByRole('heading', { name: 'Edit scheme' }),
    ).toBeTruthy()
  })
})
