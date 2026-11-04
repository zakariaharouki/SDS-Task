import type {
  CreateSchemeRequest,
  Scheme,
  SchemeStatus,
  TenureLine,
  TenureMix,
} from '../../types/scheme'
import { validateAverageValue, validateHomes } from './validation'

export const TENURES = [
  { key: 'socialRent', label: 'Social rent', affordable: true },
  { key: 'affordableRent', label: 'Affordable rent', affordable: true },
  { key: 'sharedOwnership', label: 'Shared ownership', affordable: true },
  { key: 'marketSale', label: 'Market sale', affordable: false },
] as const

export type TenureKey = (typeof TENURES)[number]['key']

// Inputs hold exactly what was typed, so "2.5" homes can be rejected instead of rounded.
export interface TenureLineValues {
  homes: string
  averageValue: string
}

export interface SchemeFormValues {
  name: string
  siteAddress: string
  status: SchemeStatus
  tenureMix: Record<TenureKey, TenureLineValues>
}

const emptyLine: TenureLineValues = { homes: '0', averageValue: '0' }

export const NEW_SCHEME_VALUES: SchemeFormValues = {
  name: '',
  siteAddress: '',
  status: 'Appraisal',
  tenureMix: {
    socialRent: emptyLine,
    affordableRent: emptyLine,
    sharedOwnership: emptyLine,
    marketSale: emptyLine,
  },
}

export const toFormValues = ({
  name,
  siteAddress,
  status,
  tenureMix,
}: Scheme): SchemeFormValues => {
  const lines = {} as SchemeFormValues['tenureMix']

  TENURES.forEach(({ key }) => {
    lines[key] = {
      homes: String(tenureMix[key].homes),
      averageValue: String(tenureMix[key].averageValue),
    }
  })

  return { name, siteAddress, status, tenureMix: lines }
}

const toTenureLine = ({ homes, averageValue }: TenureLineValues): TenureLine => ({
  homes: Number(homes.trim()),
  averageValue: Number(averageValue.trim()),
})

export const toRequest = ({
  name,
  siteAddress,
  status,
  tenureMix,
}: SchemeFormValues): CreateSchemeRequest => {
  const mix = {} as TenureMix

  TENURES.forEach(({ key }) => {
    mix[key] = toTenureLine(tenureMix[key])
  })

  return {
    name: name.trim(),
    siteAddress: siteAddress.trim(),
    status,
    tenureMix: mix,
  }
}

const hasValidHomes = ({ homes }: TenureLineValues) =>
  validateHomes(homes) === true

const hasValidValue = ({ averageValue }: TenureLineValues) =>
  validateAverageValue(averageValue, '0') === true

// JavaScript has no decimal type, so the preview adds up whole pence, which stays
// exact at the largest allowed values. The saved figures always come from the API.
const linePence = (line: TenureLineValues) =>
  Number(line.homes.trim()) * Math.round(Number(line.averageValue.trim()) * 100)

export const previewLineValue = (line: TenureLineValues): number | null =>
  hasValidHomes(line) && hasValidValue(line) ? linePence(line) / 100 : null

export interface SchemePreview {
  totalHomes: number | null
  affordableHomes: number | null
  totalValue: number | null
}

export const previewScheme = (
  tenureMix: SchemeFormValues['tenureMix'],
): SchemePreview => {
  const lines = TENURES.map(({ key, affordable }) => ({
    line: tenureMix[key],
    affordable,
  }))

  if (!lines.every(({ line }) => hasValidHomes(line))) {
    return { totalHomes: null, affordableHomes: null, totalValue: null }
  }

  const sumHomes = (items: typeof lines) =>
    items.reduce((total, { line }) => total + Number(line.homes.trim()), 0)

  return {
    totalHomes: sumHomes(lines),
    affordableHomes: sumHomes(lines.filter(({ affordable }) => affordable)),
    totalValue: lines.every(({ line }) => hasValidValue(line))
      ? lines.reduce((total, { line }) => total + linePence(line), 0) / 100
      : null,
  }
}

const FIELD_PATHS = new Set<string>([
  'name',
  'siteAddress',
  'status',
  ...TENURES.flatMap(({ key }) => [
    `tenureMix.${key}.homes`,
    `tenureMix.${key}.averageValue`,
  ]),
])

// Whether an error key from the API belongs to one input on the form.
export const isFormField = (key: string) => FIELD_PATHS.has(key)
