import { useLocation, useNavigate } from 'react-router'

// Lets tests read the address bar and use the browser's back button.
export default function CurrentAddress() {
  const { pathname, search, state } = useLocation()
  const navigate = useNavigate()

  return (
    <>
      <span data-testid="address" data-state={JSON.stringify(state)}>
        {pathname + search}
      </span>
      <button type="button" onClick={() => navigate(-1)}>
        Browser back
      </button>
    </>
  )
}
