const money = new Intl.NumberFormat('en-GB', {
  style: 'currency',
  currency: 'GBP',
})

const count = new Intl.NumberFormat('en-GB')

export const formatMoney = (value: number) => money.format(value)

export const formatNumber = (value: number) => count.format(value)

export const formatPercent = (value: number | null) =>
  value === null ? 'n/a' : `${value.toFixed(1)}%`
