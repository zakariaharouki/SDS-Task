import type { SortDir, SortField } from '../api/schemeQuery'
import { formatMoney, formatNumber, formatPercent } from '../lib/format'
import type { Scheme } from '../types/scheme'
import StatusBadge from './StatusBadge'

interface SchemesTableProps {
  schemes: Scheme[]
  onEdit: (scheme: Scheme) => void
  sortBy: SortField
  sortDir: SortDir
  onSort: (field: SortField) => void
}

interface SortableHeaderProps {
  field: SortField
  label: string
  sortBy: SortField
  sortDir: SortDir
  onSort: (field: SortField) => void
}

function SortableHeader({
  field,
  label,
  sortBy,
  sortDir,
  onSort,
}: SortableHeaderProps) {
  const active = sortBy === field

  return (
    <th
      scope="col"
      aria-sort={
        active ? (sortDir === 'asc' ? 'ascending' : 'descending') : undefined
      }
    >
      <button type="button" onClick={() => onSort(field)}>
        {label}
        <span aria-hidden="true" className="sort-arrow">
          {active ? (sortDir === 'asc' ? '▲' : '▼') : ''}
        </span>
      </button>
    </th>
  )
}

export default function SchemesTable({
  schemes,
  onEdit,
  sortBy,
  sortDir,
  onSort,
}: SchemesTableProps) {
  if (schemes.length === 0) {
    return <p>No schemes yet. Add your first one.</p>
  }

  const sort = { sortBy, sortDir, onSort }

  return (
    <div className="table-container">
      <table className="schemes-table">
        <caption>Housing schemes</caption>

        <thead>
          <tr>
            <SortableHeader field="name" label="Name" {...sort} />
            <th scope="col">Site address</th>
            <SortableHeader field="status" label="Status" {...sort} />
            <SortableHeader field="totalHomes" label="Total homes" {...sort} />
            <th scope="col">Affordable %</th>
            <SortableHeader field="totalValue" label="Total value" {...sort} />
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

              <td>{formatNumber(scheme.totalHomes)}</td>

              <td>{formatPercent(scheme.affordablePercent)}</td>

              <td>{formatMoney(scheme.totalValue)}</td>

              <td>
                <button
                  type="button"
                  onClick={() => onEdit(scheme)}
                  aria-label={`Edit ${scheme.name}, ${scheme.siteAddress}`}
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
