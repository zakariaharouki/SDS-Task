import type { SchemeStatus } from '../../types/scheme'

// The same rules and messages as the API (design section 3), for instant feedback.
// The API is still the source of truth.
const MAX_NAME_LENGTH = 200
const MAX_ADDRESS_LENGTH = 300
const MAX_HOMES = 10000
const MAX_AVERAGE_VALUE = 10000000

const HOMES_RANGE_MESSAGE = 'Homes must be between 0 and 10,000.'
const VALUE_RANGE_MESSAGE = 'Average value must be between £0 and £10,000,000.'

const NUMBER_PATTERN = /^-?(\d+(\.\d*)?|\.\d+)$/
const WHOLE_NUMBER_PATTERN = /^-?\d+$/
const TWO_DECIMALS_PATTERN = /^-?\d*(\.\d{0,2})?$/

export const validateName = (value: string) => {
  const name = value.trim()

  if (!name) {
    return 'Name is required.'
  }

  return (
    name.length <= MAX_NAME_LENGTH ||
    `Name must be ${MAX_NAME_LENGTH} characters or fewer.`
  )
}

export const validateSiteAddress = (value: string) => {
  const address = value.trim()

  if (!address) {
    return 'Site address is required.'
  }

  return (
    address.length <= MAX_ADDRESS_LENGTH ||
    `Site address must be ${MAX_ADDRESS_LENGTH} characters or fewer.`
  )
}

export const validateHomes = (value: string) => {
  const homes = value.trim()

  if (!WHOLE_NUMBER_PATTERN.test(homes)) {
    return 'Homes must be a whole number.'
  }

  const count = Number(homes)

  return (count >= 0 && count <= MAX_HOMES) || HOMES_RANGE_MESSAGE
}

export const validateAverageValue = (value: string, homes: string) => {
  const averageValue = value.trim()

  if (!NUMBER_PATTERN.test(averageValue)) {
    return 'Average value must be a number.'
  }

  const amount = Number(averageValue)

  if (amount < 0 || amount > MAX_AVERAGE_VALUE) {
    return VALUE_RANGE_MESSAGE
  }

  if (!TWO_DECIMALS_PATTERN.test(averageValue)) {
    return 'Average value can have at most 2 decimal places.'
  }

  return (
    amount > 0 ||
    validateHomes(homes) !== true ||
    Number(homes) === 0 ||
    'Average value must be greater than £0 when there are homes.'
  )
}

// Returns null while a homes field is invalid, because that field shows its own error.
export const validateTotalHomes = (
  status: SchemeStatus,
  homes: string[],
): string | null =>
  status !== 'Appraisal' &&
  homes.every((value) => validateHomes(value) === true && Number(value) === 0)
    ? 'A scheme in Planning, On site or Complete must have at least 1 home.'
    : null
