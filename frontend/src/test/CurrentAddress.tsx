import { useLocation } from 'react-router'

// Lets tests read the current address, as a person would in the address bar.
export default function CurrentAddress() {
  const { pathname, search } = useLocation()
  return <span data-testid="address">{pathname + search}</span>
}
