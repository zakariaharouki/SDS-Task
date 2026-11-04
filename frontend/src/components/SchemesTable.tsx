import type { Scheme } from '../types/scheme'
import StatusBadge from './StatusBadge'

interface SchemesTableProps {
  schemes: Scheme[]
  onEdit: (scheme: Scheme) => void
}

export default function SchemesTable({
  schemes,
  onEdit,
}: SchemesTableProps) {
  const money = new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })

  const number = new Intl.NumberFormat('en-GB')

  if (schemes.length === 0) {
    return <p>No schemes found.</p>
  }

  return (
    <div className="table-container">
      <table className="schemes-table">
        <caption>Housing schemes</caption>

        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Site address</th>
            <th scope="col">Status</th>
            <th scope="col">Total homes</th>
            <th scope="col">Affordable</th>
            <th scope="col">Total value</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>

        <tbody>
          {schemes.map((scheme) => (
            <tr key={scheme.id}>
              <th scope="row">{scheme.name}</th>
              <td>{scheme.siteAddress}</td>

              <td>
                <StatusBadge status={scheme.status} />
              </td>

              <td>{number.format(scheme.totalHomes)}</td>

              <td>
                {scheme.affordablePercent === null
                  ? 'n/a'
                  : `${scheme.affordablePercent.toFixed(1)}%`}
              </td>

              <td>{money.format(scheme.totalValue)}</td>

              <td>
                <button
                  type="button"
                  onClick={() => onEdit(scheme)}
                  aria-label={`Edit ${scheme.name}`}
                >
                  Edit
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}