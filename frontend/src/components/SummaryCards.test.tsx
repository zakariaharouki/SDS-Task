import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { SchemeSummary } from '../types/scheme'
import SummaryCards from './SummaryCards'

const cardValue = (label: string) =>
  screen.getByText(label).parentElement?.querySelector('strong')?.textContent

describe('SummaryCards', () => {
  it('puts each figure on the card with the right label', () => {
    const summary: SchemeSummary = {
      schemeCount: 3,
      totalHomes: 40000,
      affordableHomes: 10000,
      affordablePercent: 25,
      totalValue: 1234567,
      byStatus: [],
    }
    render(<SummaryCards summary={summary} />)

    expect(
      within(screen.getByRole('region', { name: 'Portfolio summary' })).getAllByText(
        /./,
        { selector: 'strong' },
      ),
    ).toHaveLength(5)
    expect(cardValue('Schemes')).toBe('3')
    expect(cardValue('Total homes')).toBe('40,000')
    expect(cardValue('Affordable homes')).toBe('10,000')
    expect(cardValue('Affordable percentage')).toBe('25.0%')
    expect(cardValue('Total value')).toBe('£1,234,567.00')
  })

  it('shows zeros and n/a for an empty portfolio', () => {
    const summary: SchemeSummary = {
      schemeCount: 0,
      totalHomes: 0,
      affordableHomes: 0,
      affordablePercent: null,
      totalValue: 0,
      byStatus: [],
    }
    render(<SummaryCards summary={summary} />)

    expect(cardValue('Schemes')).toBe('0')
    expect(cardValue('Total homes')).toBe('0')
    expect(cardValue('Affordable homes')).toBe('0')
    expect(cardValue('Affordable percentage')).toBe('n/a')
    expect(cardValue('Total value')).toBe('£0.00')
  })
})
