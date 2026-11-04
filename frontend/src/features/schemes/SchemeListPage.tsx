import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { getSchemes, getSchemeSummary } from '../../api/client'
import ErrorBanner from '../../components/ErrorBanner'
import LoadingState from '../../components/LoadingState'
import SchemesTable from '../../components/SchemesTable'
import SummaryCards from '../../components/SummaryCards'

export default function SchemeListPage() {
  const navigate = useNavigate()
  const schemes = useQuery({
    queryKey: ['schemes', 'list'],
    queryFn: getSchemes,
  })
  const summary = useQuery({
    queryKey: ['schemes', 'summary'],
    queryFn: getSchemeSummary,
  })

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
      <>
        <SummaryCards summary={summary.data} />
        <SchemesTable
          schemes={schemes.data}
          onEdit={(scheme) => navigate(`/schemes/${scheme.id}/edit`)}
        />
      </>
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
      {content}
    </main>
  )
}
