import type {
  ProblemDetails,
  Scheme,
  SchemeStatus,
  SchemeSummary,
} from '../types/scheme'
import {
  DEFAULT_SCHEME_QUERY,
  toSearchParams,
  type SchemeQuery,
} from './schemeQuery'

export class ApiError extends Error {
  // null when no response arrived, for example when the server can't be reached.
  readonly status: number | null
  readonly problem: ProblemDetails | null

  constructor(
    message: string,
    status: number | null,
    problem: ProblemDetails | null = null,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

const apiUrl = (path: string) => {
  const baseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

  if (!baseUrl) {
    throw new ApiError('The API address is not configured.', null)
  }

  return `${baseUrl.replace(/\/+$/, '')}${path}`
}

const readJson = async (response: Response): Promise<unknown> => {
  try {
    return await response.json()
  } catch {
    return null
  }
}

const asProblem = (body: unknown): ProblemDetails | null =>
  typeof body === 'object' &&
  body !== null &&
  typeof (body as ProblemDetails).title === 'string'
    ? (body as ProblemDetails)
    : null

const getJson = async <T>(path: string): Promise<T> => {
  const url = apiUrl(path)
  let response: Response

  try {
    response = await fetch(url)
  } catch {
    throw new ApiError(
      'Could not reach the server. Check your connection and try again.',
      null,
    )
  }

  const body = await readJson(response)

  if (!response.ok) {
    const problem = asProblem(body)
    throw new ApiError(
      problem?.detail ??
        problem?.title ??
        'Something went wrong. Please try again.',
      response.status,
      problem,
    )
  }

  // A wrong API address can return a web page with a 200, which isn't JSON.
  if (body === null) {
    throw new ApiError(
      'The server sent an unexpected response.',
      response.status,
    )
  }

  return body as T
}

const withParams = (path: string, params: URLSearchParams) => {
  const query = params.toString()
  return query ? `${path}?${query}` : path
}

export const getSchemes = (query: SchemeQuery = DEFAULT_SCHEME_QUERY) =>
  getJson<Scheme[]>(withParams('/api/schemes', toSearchParams(query)))

export const getSchemeSummary = (statuses: SchemeStatus[] = []) =>
  getJson<SchemeSummary>(
    withParams(
      '/api/schemes/summary',
      toSearchParams({ ...DEFAULT_SCHEME_QUERY, statuses }),
    ),
  )
