import { describe, expect, it } from 'vitest'
import {
  isFormField,
  NEW_SCHEME_VALUES,
  previewLineValue,
  previewScheme,
  toFormValues,
  toRequest,
  type SchemeFormValues,
} from './schemeFormValues'
import { makeScheme } from '../../test/schemes'

const mix = (
  socialRent: [string, string],
  affordableRent: [string, string],
  sharedOwnership: [string, string],
  marketSale: [string, string],
): SchemeFormValues['tenureMix'] => {
  const line = ([homes, averageValue]: [string, string]) => ({
    homes,
    averageValue,
  })

  return {
    socialRent: line(socialRent),
    affordableRent: line(affordableRent),
    sharedOwnership: line(sharedOwnership),
    marketSale: line(marketSale),
  }
}

const zero: [string, string] = ['0', '0']

describe('NEW_SCHEME_VALUES', () => {
  it('starts at Appraisal with every tenure at 0', () => {
    expect(NEW_SCHEME_VALUES.status).toBe('Appraisal')
    expect(toRequest(NEW_SCHEME_VALUES).tenureMix).toEqual({
      socialRent: { homes: 0, averageValue: 0 },
      affordableRent: { homes: 0, averageValue: 0 },
      sharedOwnership: { homes: 0, averageValue: 0 },
      marketSale: { homes: 0, averageValue: 0 },
    })
  })
})

describe('toFormValues', () => {
  it('turns a saved scheme into form text that sends back the same figures', () => {
    const scheme = makeScheme({
      name: 'Riverside',
      siteAddress: 'Leeds',
      status: 'Complete',
      tenureMix: {
        socialRent: { homes: 10, averageValue: 150000.5 },
        affordableRent: { homes: 0, averageValue: 0 },
        sharedOwnership: { homes: 5, averageValue: 0.07 },
        marketSale: { homes: 30, averageValue: 1234567.89 },
      },
    })

    const values = toFormValues(scheme)

    expect(values.tenureMix.socialRent).toEqual({
      homes: '10',
      averageValue: '150000.5',
    })
    expect(toRequest(values)).toEqual({
      name: 'Riverside',
      siteAddress: 'Leeds',
      status: 'Complete',
      tenureMix: scheme.tenureMix,
    })
  })
})

describe('toRequest', () => {
  it('trims text, converts numbers and sends only the fields the API accepts', () => {
    const request = toRequest({
      name: '  Riverside  ',
      siteAddress: ' Leeds ',
      status: 'Planning',
      tenureMix: mix([' 5 ', ' 123456.78 '], ['1', '2'], ['3', '4'], ['5', '6']),
    })

    expect(request).toEqual({
      name: 'Riverside',
      siteAddress: 'Leeds',
      status: 'Planning',
      tenureMix: {
        socialRent: { homes: 5, averageValue: 123456.78 },
        affordableRent: { homes: 1, averageValue: 2 },
        sharedOwnership: { homes: 3, averageValue: 4 },
        marketSale: { homes: 5, averageValue: 6 },
      },
    })
  })

  it('sends two decimal places exactly as typed', () => {
    const request = toRequest({
      ...NEW_SCHEME_VALUES,
      tenureMix: mix(['1', '0.07'], ['1', '1234567.89'], ['1', '19.99'], zero),
    })

    expect(JSON.stringify(request.tenureMix.socialRent)).toContain('0.07')
    expect(JSON.stringify(request.tenureMix.affordableRent)).toContain('1234567.89')
    expect(JSON.stringify(request.tenureMix.sharedOwnership)).toContain('19.99')
  })
})

describe('previewLineValue', () => {
  it.each([
    ['3', '100', 300],
    ['0', '100', 0],
    ['3', '0.07', 0.21],
    ['3', '1234567.89', 3703703.67],
    ['3', '19.99', 59.97],
    ['10000', '10000000', 100000000000],
    ['10000', '9999999.99', 99999999900],
  ])('%s homes at %s gives %s', (homes, averageValue, expected) => {
    expect(previewLineValue({ homes, averageValue })).toBe(expected)
  })

  it.each([
    ['abc', '100'],
    ['2.5', '100'],
    ['', '100'],
    ['3', 'abc'],
    ['3', ''],
    ['3', '-1'],
  ])('%j homes at %j has no value yet', (homes, averageValue) => {
    expect(previewLineValue({ homes, averageValue })).toBeNull()
  })
})

describe('previewScheme', () => {
  it('counts market sale in total homes but not affordable homes', () => {
    expect(
      previewScheme(mix(['1', '10'], ['2', '10'], ['3', '10'], ['4', '10'])),
    ).toEqual({ totalHomes: 10, affordableHomes: 6, totalValue: 100 })
  })

  it('adds money without floating point errors', () => {
    expect(
      previewScheme(mix(['1', '0.1'], ['1', '0.2'], zero, zero)).totalValue,
    ).toBe(0.3)
  })

  it('stays exact at the largest allowed values', () => {
    const largest: [string, string] = ['10000', '10000000']

    expect(previewScheme(mix(largest, largest, largest, largest))).toEqual({
      totalHomes: 40000,
      affordableHomes: 30000,
      totalValue: 400000000000,
    })
  })

  it('shows nothing for homes while a homes field is invalid', () => {
    expect(previewScheme(mix(['abc', '1'], zero, zero, zero))).toEqual({
      totalHomes: null,
      affordableHomes: null,
      totalValue: null,
    })
  })

  it('still counts homes while only a value is invalid', () => {
    expect(previewScheme(mix(['2', 'abc'], zero, zero, zero))).toEqual({
      totalHomes: 2,
      affordableHomes: 2,
      totalValue: null,
    })
  })
})

describe('isFormField', () => {
  it.each([
    'name',
    'siteAddress',
    'status',
    'tenureMix.socialRent.homes',
    'tenureMix.marketSale.averageValue',
  ])('%s is on the form', (key) => {
    expect(isFormField(key)).toBe(true)
  })

  it.each(['tenureMix', 'tenureMix.marketSale', 'body', 'id', 'version'])(
    '%s is not one input',
    (key) => {
      expect(isFormField(key)).toBe(false)
    },
  )
})
