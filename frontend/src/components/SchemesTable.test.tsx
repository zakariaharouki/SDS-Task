import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { makeScheme } from '../test/schemes'
import SchemesTable from './SchemesTable'

describe('SchemesTable', () => {
  it('shows a scheme with its formatted figures', () => {
    render(<SchemesTable schemes={[makeScheme()]} onEdit={() => {}} />)

    const row = within(screen.getByRole('row', { name: /The Old Depot/ }))

    expect(row.getByRole('rowheader').textContent).toBe('The Old Depot')
    expect(row.getByText('Station Yard, York')).toBeTruthy()
    expect(row.getByText('Planning')).toBeTruthy()
    expect(row.getByText('20')).toBeTruthy()
    expect(row.getByText('50.0%')).toBeTruthy()
    expect(row.getByText('£2,469,135.60')).toBeTruthy()
  })

  it('shows n/a when the percentage is null', () => {
    render(
      <SchemesTable
        schemes={[makeScheme({ affordablePercent: null })]}
        onEdit={() => {}}
      />,
    )

    expect(screen.getByText('n/a')).toBeTruthy()
  })

  it('labels the table and every column', () => {
    render(<SchemesTable schemes={[makeScheme()]} onEdit={() => {}} />)

    expect(screen.getByRole('table', { name: 'Housing schemes' })).toBeTruthy()
    expect(
      screen.getAllByRole('columnheader').map((header) => header.textContent),
    ).toEqual([
      'Name',
      'Site address',
      'Status',
      'Total homes',
      'Affordable %',
      'Total value',
      'Actions',
    ])
  })

  it('passes the chosen scheme to onEdit', () => {
    const onEdit = vi.fn()
    const leeds = makeScheme({ id: 'a', siteAddress: 'Leeds' })
    const york = makeScheme({ id: 'b', siteAddress: 'York' })
    render(<SchemesTable schemes={[leeds, york]} onEdit={onEdit} />)

    fireEvent.click(
      screen.getByRole('button', { name: 'Edit The Old Depot, York' }),
    )

    expect(onEdit).toHaveBeenCalledTimes(1)
    expect(onEdit).toHaveBeenCalledWith(york)
  })

  it('tells apart the Edit buttons of schemes with the same name', () => {
    render(
      <SchemesTable
        schemes={[
          makeScheme({ id: 'a', siteAddress: 'Leeds' }),
          makeScheme({ id: 'b', siteAddress: 'York' }),
        ]}
        onEdit={() => {}}
      />,
    )

    expect(
      screen.getByRole('button', { name: 'Edit The Old Depot, Leeds' }),
    ).toBeTruthy()
    expect(
      screen.getByRole('button', { name: 'Edit The Old Depot, York' }),
    ).toBeTruthy()
  })

  it('shows markup in a name as plain text', () => {
    const name = '<img src=x onerror=alert(1)>'
    render(<SchemesTable schemes={[makeScheme({ name })]} onEdit={() => {}} />)

    expect(screen.getByText(name)).toBeTruthy()
    expect(document.querySelector('img')).toBeNull()
  })

  it('shows the empty message instead of a table when there are no schemes', () => {
    render(<SchemesTable schemes={[]} onEdit={() => {}} />)

    expect(screen.getByText('No schemes yet. Add your first one.')).toBeTruthy()
    expect(screen.queryByRole('table')).toBeNull()
  })
})
