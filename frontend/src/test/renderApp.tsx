import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import App from '../App'
import CurrentAddress from './CurrentAddress'

export const renderApp = (route = '/') => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <App />
        <CurrentAddress />
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return queryClient
}
