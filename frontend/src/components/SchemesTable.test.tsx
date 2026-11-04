import { fireEvent, render, screen, within } from '@testing-library/react'
import type { ComponentProps } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { makeScheme } from '../test/schemes'
import SchemesTable from './SchemesTable'

const renderTable = (
  props: Partial<ComponentProps<typeof SchemesTable>> = {},
) =>
  render(
    <SchemesTable
      schemes={[makeScheme()]}
      onEdit={() => {}}
      sortBy="status"
      sortDir="asc"
      onSort={() => {}}
      {...props}
    />,
  )

describe('SchemesTable', () => {
  it('shows a scheme with its formatted figures', () => {
    renderTable()

    const row = within(screen.getByRole('row', { name: /The Old Depot/ }))

    expect(row.getByRole('rowheader').textContent).toBe('The Old Depot')
    expect(row.getByText('Station Yard, York')).toBeTruthy()
    expect(row.getByText('Planning')).toBeTruthy()
    expect(row.getByText('20')).toBeTruthy()
    expect(row.getByText('50.0%')).toBeTruthy()
    expect(row.getByText('£2,469,135.60')).toBeTruthy()
  })

  it('shows n/a when the percentage is null', () => {
    renderTable({ schemes: [makeScheme({ affordablePercent: null })] })

    expect(screen.getByText('n/a')).toBeTruthy()
  })

  it('labels the table and every column', () => {
    renderTable()

    expect(screen.getByRole('table', { name: 'Housing schemes' })).toBeTruthy()
    expect(
      screen
        .getAllByRole('columnheader')
        .map((header) => header.textContent?.replace(/[▲▼]/, '')),
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
    renderTable({ schemes: [leeds, york], onEdit })

    fireEvent.click(
      screen.getByRole('button', { name: 'Edit The Old Depot, York' }),
    )

    expect(onEdit).toHaveBeenCalledTimes(1)
    expect(onEdit).toHaveBeenCalledWith(york)
  })

  it('tells apart the Edit buttons of schemes with the same name', () => {
    renderTable({
      schemes: [
        makeScheme({ id: 'a', siteAddress: 'Leeds' }),
        makeScheme({ id: 'b', siteAddress: 'York' }),
      ],
    })

    expect(
      screen.getByRole('button', { name: 'Edit The Old Depot, Leeds' }),
    ).toBeTruthy()
    expect(
      screen.getByRole('button', { name: 'Edit The Old Depot, York' }),
    ).toBeTruthy()
  })

  it('shows markup in a name as plain text', () => {
    const name = '<img src=x onerror=alert(1)>'
    renderTable({ schemes: [makeScheme({ name })] })

    expect(screen.getByText(name)).toBeTruthy()
    expect(document.querySelector('img')).toBeNull()
  })

  it('shows the empty message instead of a table when there are no schemes', () => {
    renderTable({ schemes: [] })

    expect(screen.getByText('No schemes yet. Add your first one.')).toBeTruthy()
    expect(screen.queryByRole('table')).toBeNull()
  })
})

describe('SchemesTable sorting', () => {
  it.each([
    ['Name', 'name'],
    ['Status', 'status'],
    ['Total homes', 'totalHomes'],
    ['Total value', 'totalValue'],
  ])('the %s heading asks to sort by %s', (label, field) => {
    const onSort = vi.fn()
    renderTable({ onSort })

    fireEvent.click(screen.getByRole('button', { name: label }))

    expect(onSort).toHaveBeenCalledTimes(1)
    expect(onSort).toHaveBeenCalledWith(field)
  })

  it.each(['Site address', 'Affordable %', 'Actions'])(
    'the %s heading has no sort button',
    (label) => {
      renderTable()

      expect(screen.queryByRole('button', { name: label })).toBeNull()
    },
  )

  it.each([
    ['asc', 'ascending', '▲'],
    ['desc', 'descending', '▼'],
  ] as const)('announces %s on the sorted column only', (sortDir, ariaSort, arrow) => {
    renderTable({ sortBy: 'totalValue', sortDir })

    const sorted = screen.getByRole('columnheader', { name: 'Total value' })

    expect(sorted.getAttribute('aria-sort')).toBe(ariaSort)
    expect(sorted.textContent).toContain(arrow)
    expect(document.querySelectorAll('th[aria-sort]')).toHaveLength(1)
  })
})
