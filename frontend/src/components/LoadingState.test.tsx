import { act, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import LoadingState from './LoadingState'

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('LoadingState', () => {
  it('says Loading at first and until just before the delay', () => {
    render(<LoadingState />)

    expect(screen.getByRole('status').textContent).toBe('Loading…')

    act(() => {
      vi.advanceTimersByTime(2999)
    })

    expect(screen.getByRole('status').textContent).toBe('Loading…')
  })

  it('says the server is waking up after a few seconds', () => {
    render(<LoadingState />)

    act(() => {
      vi.advanceTimersByTime(3000)
    })

    expect(screen.getByRole('status').textContent).toContain(
      'Waking up the server…',
    )
  })

  it('cancels its timer when it goes away', () => {
    const { unmount } = render(<LoadingState />)

    unmount()

    expect(vi.getTimerCount()).toBe(0)
  })
})
