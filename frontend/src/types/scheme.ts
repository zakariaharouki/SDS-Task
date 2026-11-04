export type SchemeStatus =
  | 'Appraisal'
  | 'Planning'
  | 'OnSite'
  | 'Complete'

export interface TenureLine {
  homes: number
  averageValue: number
}

export interface TenureMix {
  socialRent: TenureLine
  affordableRent: TenureLine
  sharedOwnership: TenureLine
  marketSale: TenureLine
}

export interface Scheme {
  id: string
  name: string
  siteAddress: string
  status: SchemeStatus
  tenureMix: TenureMix
  totalHomes: number
  affordableHomes: number
  affordablePercent: number | null
  totalValue: number
  version: number
  createdAt: string
  updatedAt: string
}

export interface SchemeStatusSummary {
  status: SchemeStatus
  schemeCount: number
  totalHomes: number
  affordableHomes: number
  affordablePercent: number | null
  totalValue: number
}

export interface SchemeSummary {
  schemeCount: number
  totalHomes: number
  affordableHomes: number
  affordablePercent: number | null
  totalValue: number
  byStatus: SchemeStatusSummary[]
}