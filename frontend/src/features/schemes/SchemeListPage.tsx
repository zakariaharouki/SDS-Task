import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router'
import { getSchemes, getSchemeSummary } from '../../api/client'
import {
  isFiltered,
  nextSort,
  parseSchemeQuery,
  toSearchParams,
  type SchemeQuery,
} from '../../api/schemeQuery'
import ErrorBanner from '../../components/ErrorBanner'
import LoadingState from '../../components/LoadingState'
import SchemesTable from '../../components/SchemesTable'
import StatusFilter from '../../components/StatusFilter'
import SummaryCards from '../../components/SummaryCards'

export default function SchemeListPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const query = parseSchemeQuery(searchParams)

  // The current figures stay on screen while a new filter or sort loads.
  const schemes = useQuery({
    queryKey: ['schemes', 'list', query],
    queryFn: () => getSchemes(query),
    placeholderData: keepPreviousData,
  })
  const summary = useQuery({
    queryKey: ['schemes', 'summary', query.statuses],
    queryFn: () => getSchemeSummary(query.statuses),
    placeholderData: keepPreviousData,
  })

  const updateQuery = (next: SchemeQuery) => setSearchParams(toSearchParams(next))

  const retry = () => {
    if (!schemes.data) {
      void schemes.refetch()
    }
    if (!summary.data) {
      void summary.refetch()
    }
  }

  let content

  // Figures already on screen stay there if a background refresh fails.
  if (schemes.data && summary.data) {
    content = (
      <div
        className="scheme-results"
        aria-busy={schemes.isPlaceholderData || summary.isPlaceholderData}
      >
        <SummaryCards summary={summary.data} />
        {schemes.data.length === 0 && isFiltered(query.statuses) ? (
          <div className="no-matches">
            <p>No schemes match this filter.</p>
            <button
              type="button"
              onClick={() => updateQuery({ ...query, statuses: [] })}
            >
              Clear filter
            </button>
          </div>
        ) : (
          <SchemesTable
            schemes={schemes.data}
            onEdit={(scheme) => navigate(`/schemes/${scheme.id}/edit`)}
            sortBy={query.sortBy}
            sortDir={query.sortDir}
            onSort={(field) => updateQuery(nextSort(query, field))}
          />
        )}
      </div>
    )
  } else if (schemes.isFetching || summary.isFetching) {
    content = <LoadingState />
  } else {
    content = (
      <ErrorBanner
        message={
          schemes.error?.message ??
          summary.error?.message ??
          'Something went wrong. Please try again.'
        }
        onRetry={retry}
      />
    )
  }

  return (
    <main className="page">
      <h1>Scheme Pipeline Tracker</h1>
      <StatusFilter
        selected={query.statuses}
        onChange={(statuses) => updateQuery({ ...query, statuses })}
      />
      {content}
    </main>
  )
}
