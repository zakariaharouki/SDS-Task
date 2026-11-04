import { describe, expect, it } from 'vitest'
import { formatMoney, formatNumber, formatPercent } from './format'

describe('formatMoney', () => {
  it.each([
    [0, '£0.00'],
    [1234567, '£1,234,567.00'],
    [123456.78, '£123,456.78'],
    [400000000000, '£400,000,000,000.00'],
  ])('%s shows as %s', (value, text) => {
    expect(formatMoney(value)).toBe(text)
  })
})

describe('formatNumber', () => {
  it.each([
    [0, '0'],
    [999, '999'],
    [40000, '40,000'],
  ])('%s shows as %s', (value, text) => {
    expect(formatNumber(value)).toBe(text)
  })
})

describe('formatPercent', () => {
  it.each([
    [null, 'n/a'],
    [0, '0.0%'],
    [33.3, '33.3%'],
    [100, '100.0%'],
  ])('%s shows as %s', (value, text) => {
    expect(formatPercent(value)).toBe(text)
  })
})
