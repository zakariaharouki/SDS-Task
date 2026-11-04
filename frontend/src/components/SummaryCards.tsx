import { formatMoney, formatNumber, formatPercent } from '../lib/format'
import type { SchemeSummary } from '../types/scheme'

interface SummaryCardsProps {
  summary: SchemeSummary
}

export default function SummaryCards({ summary }: SummaryCardsProps) {
  const cards = [
    {
      label: 'Schemes',
      value: formatNumber(summary.schemeCount),
    },
    {
      label: 'Total homes',
      value: formatNumber(summary.totalHomes),
    },
    {
      label: 'Affordable homes',
      value: formatNumber(summary.affordableHomes),
    },
    {
      label: 'Affordable percentage',
      value: formatPercent(summary.affordablePercent),
    },
    {
      label: 'Total value',
      value: formatMoney(summary.totalValue),
    },
  ]

  return (
    <section aria-label="Portfolio summary" className="summary-cards">
      {cards.map((card) => (
        <div key={card.label} className="summary-card">
          <p>{card.label}</p>
          <strong>{card.value}</strong>
        </div>
      ))}
    </section>
  )
}