import { isFiltered, SCHEME_STATUSES } from '../api/schemeQuery'
import { statusLabels } from '../lib/format'
import type { SchemeStatus } from '../types/scheme'

interface StatusFilterProps {
  selected: SchemeStatus[]
  onChange: (statuses: SchemeStatus[]) => void
}

export default function StatusFilter({ selected, onChange }: StatusFilterProps) {
  const toggle = (status: SchemeStatus) =>
    onChange(
      selected.includes(status)
        ? selected.filter((other) => other !== status)
        : [...selected, status],
    )

  return (
    <fieldset className="status-filter">
      <legend>Filter by status</legend>

      {SCHEME_STATUSES.map((status) => (
        <label key={status}>
          <input
            type="checkbox"
            checked={selected.includes(status)}
            onChange={() => toggle(status)}
          />
          {statusLabels[status]}
        </label>
      ))}

      {isFiltered(selected) && (
        <p>
          Showing {selected.length} of {SCHEME_STATUSES.length} statuses
        </p>
      )}
    </fieldset>
  )
}
