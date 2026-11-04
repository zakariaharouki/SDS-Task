import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import ErrorBanner from './ErrorBanner'

describe('ErrorBanner', () => {
  it('announces the message and calls onRetry from its button', () => {
    const onRetry = vi.fn()
    render(<ErrorBanner message="The API is down." onRetry={onRetry} />)

    expect(screen.getByRole('alert').textContent).toContain('The API is down.')

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(onRetry).toHaveBeenCalledTimes(1)
  })
})
