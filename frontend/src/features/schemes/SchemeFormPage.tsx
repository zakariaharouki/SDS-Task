import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { createScheme } from '../../api/client'
import { listAddress } from './listAddress'
import SchemeForm from './SchemeForm'
import { NEW_SCHEME_VALUES } from './schemeFormValues'

function AddScheme({ backTo }: { backTo: string }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
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
          void queryClient.invalidateQueries({ queryKey: ['schemes'] })
          // Replaced, so the browser's back button doesn't reopen the saved form.
          navigate(backTo, { replace: true, state: { saved: true } })
        }}
      />
    </main>
  )
}

// Placeholder until editing is built.
function EditScheme({ backTo }: { backTo: string }) {
  return (
    <main className="page">
      <h1>Edit scheme</h1>
      <p className="page-note">Editing isn't available yet.</p>
      <Link to={backTo}>Back to schemes</Link>
    </main>
  )
}

export default function SchemeFormPage() {
  const { id } = useParams()
  const location = useLocation()
  const backTo = listAddress(location.state)

  return id ? <EditScheme backTo={backTo} /> : <AddScheme backTo={backTo} />
}
