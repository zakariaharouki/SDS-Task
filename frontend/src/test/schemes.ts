import type { Scheme } from '../types/scheme'

const line = { homes: 5, averageValue: 123456.78 }

export const makeScheme = (overrides: Partial<Scheme> = {}): Scheme => ({
  id: 'scheme-1',
  name: 'The Old Depot',
  siteAddress: 'Station Yard, York',
  status: 'Planning',
  tenureMix: {
    socialRent: line,
    affordableRent: line,
    sharedOwnership: line,
    marketSale: line,
  },
  totalHomes: 20,
  affordableHomes: 10,
  affordablePercent: 50,
  totalValue: 2469135.6,
  version: 1,
  createdAt: '2026-11-04T10:00:00.000Z',
  updatedAt: '2026-11-04T10:00:00.000Z',
  ...overrides,
})
