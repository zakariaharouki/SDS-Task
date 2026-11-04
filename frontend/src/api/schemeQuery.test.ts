import { describe, expect, it } from 'vitest'
import {
  DEFAULT_SCHEME_QUERY,
  isFiltered,
  nextSort,
  parseSchemeQuery,
  toSearchParams,
} from './schemeQuery'

const parse = (search: string) => parseSchemeQuery(new URLSearchParams(search))

describe('parseSchemeQuery', () => {
  it('uses the defaults when the address has no settings', () => {
    expect(parse('')).toEqual(DEFAULT_SCHEME_QUERY)
  })

  it('reads the statuses and sort from the address', () => {
    expect(parse('status=Planning&status=OnSite&sortBy=totalValue&sortDir=desc')).toEqual({
      statuses: ['Planning', 'OnSite'],
      sortBy: 'totalValue',
      sortDir: 'desc',
    })
  })

  it('puts statuses in pipeline order and removes repeats', () => {
    expect(parse('status=Complete&status=Appraisal&status=Complete').statuses).toEqual([
      'Appraisal',
      'Complete',
    ])
  })

  it.each([
    ['lower case status', 'status=planning'],
    ['numeric status', 'status=1'],
    ['unknown status', 'status=Done'],
    ['unknown sort field', 'sortBy=price'],
    ['unknown direction', 'sortDir=sideways'],
    ['a field the table has no column for', 'sortBy=updatedAt'],
    ['an empty status', 'status='],
  ])('ignores %s', (_name, search) => {
    expect(parse(search)).toEqual(DEFAULT_SCHEME_QUERY)
  })

  it('keeps the good values when others are bad', () => {
    expect(parse('status=Planning&status=nope&sortBy=name&sortDir=up')).toEqual({
      statuses: ['Planning'],
      sortBy: 'name',
      sortDir: 'asc',
    })
  })
})

describe('toSearchParams', () => {
  it('leaves out defaults so a plain address stays plain', () => {
    expect(toSearchParams(DEFAULT_SCHEME_QUERY).toString()).toBe('')
  })

  it('repeats status, in pipeline order, however it was chosen', () => {
    expect(
      toSearchParams({
        ...DEFAULT_SCHEME_QUERY,
        statuses: ['Complete', 'Planning'],
      }).toString(),
    ).toBe('status=Planning&status=Complete')
  })

  it('includes a sort that differs from the default', () => {
    expect(
      toSearchParams({
        statuses: [],
        sortBy: 'totalValue',
        sortDir: 'desc',
      }).toString(),
    ).toBe('sortBy=totalValue&sortDir=desc')
  })

  it('keeps a descending sort on the default field', () => {
    expect(
      toSearchParams({ ...DEFAULT_SCHEME_QUERY, sortDir: 'desc' }).toString(),
    ).toBe('sortDir=desc')
  })

  it('gives back the same settings when read again', () => {
    const query = {
      statuses: ['Appraisal' as const, 'OnSite' as const],
      sortBy: 'name' as const,
      sortDir: 'desc' as const,
    }

    expect(parseSchemeQuery(toSearchParams(query))).toEqual(query)
  })
})

describe('isFiltered', () => {
  it.each([
    [[], false],
    [['Planning'], true],
    [['Appraisal', 'Planning', 'OnSite'], true],
    [['Appraisal', 'Planning', 'OnSite', 'Complete'], false],
  ] as const)('%j gives %s', (statuses, expected) => {
    expect(isFiltered([...statuses])).toBe(expected)
  })
})

describe('nextSort', () => {
  it('starts a new column ascending, even if the last one was descending', () => {
    expect(
      nextSort({ ...DEFAULT_SCHEME_QUERY, sortDir: 'desc' }, 'name'),
    ).toMatchObject({ sortBy: 'name', sortDir: 'asc' })
  })

  it('flips the current column both ways', () => {
    const descending = nextSort(DEFAULT_SCHEME_QUERY, 'status')

    expect(descending.sortDir).toBe('desc')
    expect(nextSort(descending, 'status').sortDir).toBe('asc')
  })

  it('keeps the statuses', () => {
    expect(
      nextSort({ ...DEFAULT_SCHEME_QUERY, statuses: ['Planning'] }, 'name').statuses,
    ).toEqual(['Planning'])
  })
})
