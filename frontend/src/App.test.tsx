import { fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderApp } from './test/renderApp'

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})))
  vi.stubEnv('VITE_API_BASE_URL', 'https://api.test')
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
})

describe('routes', () => {
  it('/ shows the list page', () => {
    renderApp('/')

    expect(
      screen.getByRole('heading', { name: 'Scheme Pipeline Tracker' }),
    ).toBeTruthy()
  })

  it.each(['/schemes/new', '/schemes/abc/edit'])(
    '%s shows the form page, with a way back to the list',
    (route) => {
      renderApp(route)

      expect(screen.getByRole('heading', { name: 'Scheme form' })).toBeTruthy()

      fireEvent.click(screen.getByRole('link', { name: 'Back to schemes' }))

      expect(
        screen.getByRole('heading', { name: 'Scheme Pipeline Tracker' }),
      ).toBeTruthy()
    },
  )

  it('an unknown address shows page not found', () => {
    renderApp('/nope')

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Back to schemes' })).toBeTruthy()
  })
})
