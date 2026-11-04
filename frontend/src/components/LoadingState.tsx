import { useEffect, useState } from 'react'

// The API's free hosting sleeps when idle and can take up to a minute to wake.
const WAKING_DELAY_MS = 3000

export default function LoadingState() {
  const [waking, setWaking] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => setWaking(true), WAKING_DELAY_MS)
    return () => clearTimeout(timer)
  }, [])

  return (
    <p role="status" className="page-note">
      {waking
        ? 'Waking up the server… This can take up to a minute.'
        : 'Loading…'}
    </p>
  )
}
