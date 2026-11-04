import { describe, expect, it } from 'vitest'
import {
  validateAverageValue,
  validateHomes,
  validateName,
  validateSiteAddress,
  validateTotalHomes,
} from './validation'

const WHOLE = 'Homes must be a whole number.'
const HOMES_RANGE = 'Homes must be between 0 and 10,000.'
const NUMBER = 'Average value must be a number.'
const VALUE_RANGE = 'Average value must be between £0 and £10,000,000.'
const DECIMALS = 'Average value can have at most 2 decimal places.'
const NEEDS_VALUE = 'Average value must be greater than £0 when there are homes.'
const NEEDS_HOME =
  'A scheme in Planning, On site or Complete must have at least 1 home.'

describe('validateName', () => {
  it.each([
    ['', 'Name is required.'],
    ['   ', 'Name is required.'],
    ['a'.repeat(201), 'Name must be 200 characters or fewer.'],
    ['Riverside', true],
    ['a'.repeat(200), true],
    [`  ${'a'.repeat(200)}  `, true],
  ])('%j gives %s', (value, expected) => {
    expect(validateName(value)).toBe(expected)
  })
})

describe('validateSiteAddress', () => {
  it.each([
    ['', 'Site address is required.'],
    ['   ', 'Site address is required.'],
    ['a'.repeat(301), 'Site address must be 300 characters or fewer.'],
    ['Leeds', true],
    ['a'.repeat(300), true],
    [`  ${'a'.repeat(300)}  `, true],
  ])('%j gives %s', (value, expected) => {
    expect(validateSiteAddress(value)).toBe(expected)
  })
})

describe('validateHomes', () => {
  it.each([
    ['0', true],
    ['1', true],
    ['10000', true],
    [' 25 ', true],
    ['', WHOLE],
    ['2.5', WHOLE],
    ['2.0', WHOLE],
    ['1e3', WHOLE],
    ['1,000', WHOLE],
    ['abc', WHOLE],
    ['-1', HOMES_RANGE],
    ['10001', HOMES_RANGE],
    ['99999999999999999999', HOMES_RANGE],
  ])('%j gives %s', (value, expected) => {
    expect(validateHomes(value)).toBe(expected)
  })
})

describe('validateAverageValue', () => {
  it.each([
    ['0', '0', true],
    ['100', '5', true],
    ['123456.78', '5', true],
    ['1.5', '5', true],
    ['.5', '5', true],
    ['10000000', '5', true],
    ['10000000.00', '5', true],
    ['', '5', NUMBER],
    ['abc', '5', NUMBER],
    ['£100', '5', NUMBER],
    ['1,000', '5', NUMBER],
    ['1e3', '5', NUMBER],
    ['-1', '0', VALUE_RANGE],
    ['10000000.01', '5', VALUE_RANGE],
    ['1.234', '5', DECIMALS],
    ['0', '5', NEEDS_VALUE],
    ['0.00', '5', NEEDS_VALUE],
  ])('%j with %j homes gives %s', (value, homes, expected) => {
    expect(validateAverageValue(value, homes)).toBe(expected)
  })

  it('leaves the homes error to the homes field', () => {
    expect(validateAverageValue('0', 'abc')).toBe(true)
  })
})

describe('validateTotalHomes', () => {
  const none = ['0', '0', '0', '0']

  it.each(['Planning', 'OnSite', 'Complete'] as const)(
    '%s needs at least 1 home',
    (status) => {
      expect(validateTotalHomes(status, none)).toBe(NEEDS_HOME)
      expect(validateTotalHomes(status, ['0', '0', '1', '0'])).toBeNull()
    },
  )

  it('allows 0 homes at Appraisal', () => {
    expect(validateTotalHomes('Appraisal', none)).toBeNull()
  })

  it('stays quiet while a homes field is invalid', () => {
    expect(validateTotalHomes('Planning', ['abc', '0', '0', '0'])).toBeNull()
  })
})
