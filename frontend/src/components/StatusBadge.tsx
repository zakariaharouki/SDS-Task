import { statusLabels } from '../lib/format'
import type { SchemeStatus } from '../types/scheme'

interface StatusBadgeProps {
  status: SchemeStatus
}

const colors: Record<SchemeStatus, string> = {
  Appraisal: '#475569',
  Planning: '#1d4ed8',
  OnSite: '#92400e',
  Complete: '#166534',
}

export default function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span
      style={{
        display: 'inline-block',
        padding: '4px 10px',
        borderRadius: '16px',
        backgroundColor: colors[status],
        color: 'white',
        fontSize: '13px',
        whiteSpace: 'nowrap',
      }}
    >
      {statusLabels[status]}
    </span>
  )
}
