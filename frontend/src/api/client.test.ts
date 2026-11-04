import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { makeScheme } from '../test/schemes'
import type { SchemeSummary } from '../types/scheme'
import { ApiError, createScheme, getSchemes, getSchemeSummary } from './client'

const fetchMock = vi.fn<typeof fetch>()

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status })

const errorFrom = (call: Promise<unknown>) =>
  call.then(
    () => {
      throw new Error('Expected the call to fail')
    },
    (error: unknown) => {
      expect(error).toBeInstanceOf(ApiError)
      return error as ApiError
    },
  )

beforeEach(() => {
  fetchMock.mockReset()
  vi.stubGlobal('fetch', fetchMock)
  vi.stubEnv('VITE_API_BASE_URL', 'https://api.test')
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
})

describe('successful requests', () => {
  it('getSchemes returns the list from /api/schemes', async () => {
    const schemes = [makeScheme()]
    fetchMock.mockResolvedValue(json(schemes))

    await expect(getSchemes()).resolves.toEqual(schemes)
    expect(fetchMock).toHaveBeenCalledWith('https://api.test/api/schemes')
  })

  it('getSchemeSummary returns the summary from /api/schemes/summary', async () => {
    const summary: SchemeSummary = {
      schemeCount: 0,
      totalHomes: 0,
      affordableHomes: 0,
      affordablePercent: null,
      totalValue: 0,
      byStatus: [],
    }
    fetchMock.mockResolvedValue(json(summary))

    await expect(getSchemeSummary()).resolves.toEqual(summary)
    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.test/api/schemes/summary',
    )
  })

  it('returns an empty list rather than treating it as an error', async () => {
    fetchMock.mockResolvedValue(json([]))

    await expect(getSchemes()).resolves.toEqual([])
  })

  it('copes with a trailing slash on the API address', async () => {
    vi.stubEnv('VITE_API_BASE_URL', 'https://api.test/')
    fetchMock.mockResolvedValue(json([]))

    await getSchemes()

    expect(fetchMock).toHaveBeenCalledWith('https://api.test/api/schemes')
  })
})

describe('filter and sort', () => {
  it('getSchemes sends repeated statuses and the sort', async () => {
    fetchMock.mockResolvedValue(json([]))

    await getSchemes({
      statuses: ['Planning', 'OnSite'],
      sortBy: 'totalValue',
      sortDir: 'desc',
    })

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.test/api/schemes?status=Planning&status=OnSite&sortBy=totalValue&sortDir=desc',
    )
  })

  it('getSchemeSummary sends only the statuses', async () => {
    fetchMock.mockResolvedValue(json({}))

    await getSchemeSummary(['Complete'])

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.test/api/schemes/summary?status=Complete',
    )
  })
})

describe('createScheme', () => {
  const scheme = {
    name: 'Riverside',
    siteAddress: 'Leeds',
    status: 'Appraisal' as const,
    tenureMix: makeScheme().tenureMix,
  }

  it('posts the scheme as JSON and returns what the API saved', async () => {
    const created = makeScheme({ id: 'new-id' })
    fetchMock.mockResolvedValue(json(created, 201))

    await expect(createScheme(scheme)).resolves.toEqual(created)

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('https://api.test/api/schemes')
    expect(init?.method).toBe('POST')
    expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
    expect(JSON.parse(String(init?.body))).toEqual(scheme)
  })

  it('keeps the field errors from a rejected save', async () => {
    const errors = { name: ['Name is required.'] }
    fetchMock.mockResolvedValue(
      json({ type: 't', title: 'Invalid', status: 400, errors }, 400),
    )

    const error = await errorFrom(createScheme(scheme))

    expect(error.status).toBe(400)
    expect(error.problem?.errors).toEqual(errors)
  })
})

describe('error responses', () => {
  it('keeps the status and field errors from a Problem Details response', async () => {
    const problem = {
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { 'tenureMix.marketSale.homes': ['Homes must be a whole number.'] },
      traceId: '00-abc-def-00',
    }
    fetchMock.mockResolvedValue(json(problem, 400))

    const error = await errorFrom(getSchemes())

    expect(error.status).toBe(400)
    expect(error.message).toBe('One or more validation errors occurred.')
    expect(error.problem).toEqual(problem)
  })

  it('prefers the detail over the title for the message', async () => {
    fetchMock.mockResolvedValue(
      json({ type: 't', title: 'Conflict', status: 409, detail: 'Reload the scheme.' }, 409),
    )

    const error = await errorFrom(getSchemes())

    expect(error.message).toBe('Reload the scheme.')
  })

  it('gives a general message when an error response is not JSON', async () => {
    fetchMock.mockResolvedValue(
      new Response('<html>Bad gateway</html>', { status: 502 }),
    )

    const error = await errorFrom(getSchemes())

    expect(error.status).toBe(502)
    expect(error.problem).toBeNull()
    expect(error.message).toBe('Something went wrong. Please try again.')
  })

  it('does not treat other JSON as Problem Details', async () => {
    fetchMock.mockResolvedValue(json({ message: 'nope' }, 500))

    const error = await errorFrom(getSchemes())

    expect(error.status).toBe(500)
    expect(error.problem).toBeNull()
    expect(error.message).toBe('Something went wrong. Please try again.')
  })
})

describe('when no usable response arrives', () => {
  it('gives a friendly message when the server cannot be reached', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    const error = await errorFrom(getSchemes())

    expect(error.status).toBeNull()
    expect(error.message).toBe(
      'Could not reach the server. Check your connection and try again.',
    )
  })

  it('rejects a 200 that is not JSON, such as a web page from a wrong address', async () => {
    fetchMock.mockResolvedValue(new Response('<!doctype html>', { status: 200 }))

    const error = await errorFrom(getSchemes())

    expect(error.status).toBe(200)
    expect(error.message).toBe('The server sent an unexpected response.')
  })

  it.each([
    ['missing', undefined],
    ['empty', ''],
  ])('fails clearly without calling fetch when the address is %s', async (_name, value) => {
    vi.stubEnv('VITE_API_BASE_URL', value as string)

    const error = await errorFrom(getSchemes())

    expect(error.message).toBe('The API address is not configured.')
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
