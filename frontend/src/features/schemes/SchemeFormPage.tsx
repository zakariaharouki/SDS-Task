import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import {
  createScheme,
  getScheme,
  isNotFound,
  isVersionConflict,
  updateScheme,
} from '../../api/client'
import ErrorBanner from '../../components/ErrorBanner'
import LoadingState from '../../components/LoadingState'
import type { UpdateSchemeRequest } from '../../types/scheme'
import { listAddress } from './listAddress'
import SchemeForm from './SchemeForm'
import { NEW_SCHEME_VALUES, toFormValues } from './schemeFormValues'

// Refreshes the list and summary, then returns to the list with the saved message.
const useFinishSaving = (backTo: string) => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  return () => {
    void queryClient.invalidateQueries({ queryKey: ['schemes'] })
    // Replaced, so the browser's back button doesn't reopen the saved form.
    navigate(backTo, { replace: true, state: { saved: true } })
  }
}

function AddScheme({ backTo }: { backTo: string }) {
  const finishSaving = useFinishSaving(backTo)
  const create = useMutation({ mutationFn: createScheme })

  return (
    <main className="page">
      <h1>Add scheme</h1>
      <SchemeForm
        defaultValues={NEW_SCHEME_VALUES}
        submitLabel="Save scheme"
        backTo={backTo}
        onSubmit={async (scheme) => {
          await create.mutateAsync(scheme)
          finishSaving()
        }}
      />
    </main>
  )
}

function EditScheme({ id, backTo }: { id: string; backTo: string }) {
  const finishSaving = useFinishSaving(backTo)
  const [conflict, setConflict] = useState<string | null>(null)
  const [gone, setGone] = useState(false)

  // Loaded once per visit. The form holds what's being typed and the version it was
  // based on, so a refresh in the background must not change either of them.
  const scheme = useQuery({
    queryKey: ['scheme', id],
    queryFn: () => getScheme(id),
    staleTime: Infinity,
    gcTime: 0,
  })
  const update = useMutation({
    mutationFn: (request: UpdateSchemeRequest) => updateScheme(id, request),
  })

  const reloadLatest = async () => {
    const result = await scheme.refetch()

    if (!result.isError) {
      setConflict(null)
    }
  }

  let content

  if (gone || isNotFound(scheme.error)) {
    content = (
      <>
        <p className="page-note">This scheme could not be found.</p>
        <Link to={backTo}>Back to schemes</Link>
      </>
    )
  } else if (scheme.data) {
    const { version } = scheme.data

    content = (
      <SchemeForm
        // Starts the form again from the latest values after a reload.
        key={scheme.dataUpdatedAt}
        defaultValues={toFormValues(scheme.data)}
        submitLabel="Save changes"
        backTo={backTo}
        notice={
          conflict && (
            <div role="alert" className="error-banner">
              <p>{conflict}</p>
              <button
                type="button"
                disabled={scheme.isFetching}
                onClick={() => void reloadLatest()}
              >
                Reload latest version
              </button>
            </div>
          )
        }
        onSubmit={async (request) => {
          setConflict(null)

          try {
            await update.mutateAsync({ ...request, version })
          } catch (error) {
            if (isVersionConflict(error)) {
              setConflict((error as Error).message)
            } else if (isNotFound(error)) {
              setGone(true)
            } else {
              throw error
            }
            return
          }

          finishSaving()
        }}
      />
    )
  } else if (scheme.isFetching) {
    content = (
      <>
        <LoadingState />
        <Link to={backTo}>Back to schemes</Link>
      </>
    )
  } else {
    content = (
      <>
        <ErrorBanner
          message={
            scheme.error?.message ?? 'Something went wrong. Please try again.'
          }
          onRetry={() => void scheme.refetch()}
        />
        <Link to={backTo}>Back to schemes</Link>
      </>
    )
  }

  return (
    <main className="page">
      <h1>Edit scheme</h1>
      {content}
    </main>
  )
}

export default function SchemeFormPage() {
  const { id } = useParams()
  const location = useLocation()
  const backTo = listAddress(location.state)

  return id ? (
    <EditScheme id={id} backTo={backTo} />
  ) : (
    <AddScheme backTo={backTo} />
  )
}
