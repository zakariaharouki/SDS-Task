import type { SchemeStatus } from '../types/scheme'

// In pipeline order, so the filter and the address always list statuses the same way.
export const SCHEME_STATUSES: SchemeStatus[] = [
  'Appraisal',
  'Planning',
  'OnSite',
  'Complete',
]

// The API can also sort by updatedAt, but the table has no column for it.
const SORT_FIELDS = ['status', 'name', 'totalHomes', 'totalValue'] as const

export type SortField = (typeof SORT_FIELDS)[number]
export type SortDir = 'asc' | 'desc'

export interface SchemeQuery {
  statuses: SchemeStatus[]
  sortBy: SortField
  sortDir: SortDir
}

export const DEFAULT_SCHEME_QUERY: SchemeQuery = {
  statuses: [],
  sortBy: 'status',
  sortDir: 'asc',
}

// Values it doesn't recognise are ignored, so a bad link shows the normal list.
export const parseSchemeQuery = (params: URLSearchParams): SchemeQuery => {
  const statuses = params.getAll('status')
  const sortBy = params.get('sortBy')

  return {
    statuses: SCHEME_STATUSES.filter((status) => statuses.includes(status)),
    sortBy:
      SORT_FIELDS.find((field) => field === sortBy) ??
      DEFAULT_SCHEME_QUERY.sortBy,
    sortDir: params.get('sortDir') === 'desc' ? 'desc' : 'asc',
  }
}

// Used for both the page address and the API request. Defaults are left out.
export const toSearchParams = ({
  statuses,
  sortBy,
  sortDir,
}: SchemeQuery): URLSearchParams => {
  const params = new URLSearchParams()

  SCHEME_STATUSES.filter((status) => statuses.includes(status)).forEach(
    (status) => params.append('status', status),
  )

  if (sortBy !== DEFAULT_SCHEME_QUERY.sortBy) {
    params.set('sortBy', sortBy)
  }

  if (sortDir !== DEFAULT_SCHEME_QUERY.sortDir) {
    params.set('sortDir', sortDir)
  }

  return params
}

// Ticking every status shows everything, so it doesn't count as filtering.
export const isFiltered = (statuses: SchemeStatus[]) =>
  statuses.length > 0 && statuses.length < SCHEME_STATUSES.length

// A new column starts ascending. The current column flips direction.
export const nextSort = (query: SchemeQuery, field: SortField): SchemeQuery => ({
  ...query,
  sortBy: field,
  sortDir: query.sortBy === field && query.sortDir === 'asc' ? 'desc' : 'asc',
})
