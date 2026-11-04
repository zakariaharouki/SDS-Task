import { useEffect, useState } from 'react'

// The API's free hosting sleeps when idle and can take up to a minute to wake.
const WAKING_DELAY_MS = 3000

interface LoadingStateProps {
  label?: string
}

export default function LoadingState({ label = 'Loading…' }: LoadingStateProps) {
  const [waking, setWaking] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => setWaking(true), WAKING_DELAY_MS)
    return () => clearTimeout(timer)
  }, [])

  return (
    <p role="status" className="page-note">
      {waking ? 'Waking up the server… This can take up to a minute.' : label}
    </p>
  )
}
