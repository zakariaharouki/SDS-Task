import type { SchemeSummary } from '../types/scheme'

interface SummaryCardsProps {
  summary: SchemeSummary
}

export default function SummaryCards({ summary }: SummaryCardsProps) {
  const money = new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })

  const number = new Intl.NumberFormat('en-GB')

  const cards = [
    {
      label: 'Schemes',
      value: number.format(summary.schemeCount),
    },
    {
      label: 'Total homes',
      value: number.format(summary.totalHomes),
    },
    {
      label: 'Affordable homes',
      value: number.format(summary.affordableHomes),
    },
    {
      label: 'Affordable percentage',
      value:
        summary.affordablePercent === null
          ? 'n/a'
          : `${summary.affordablePercent.toFixed(1)}%`,
    },
    {
      label: 'Total value',
      value: money.format(summary.totalValue),
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