import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import StatusFilter from './StatusFilter'

describe('StatusFilter', () => {
  it('offers every status with its friendly label, in pipeline order', () => {
    render(<StatusFilter selected={[]} onChange={() => {}} />)

    expect(screen.getByRole('group', { name: 'Filter by status' })).toBeTruthy()
    expect(
      screen.getAllByRole('checkbox').map((box) => box.parentElement?.textContent),
    ).toEqual(['Appraisal', 'Planning', 'On site', 'Complete'])
  })

  it('ticks only the selected statuses', () => {
    render(<StatusFilter selected={['OnSite']} onChange={() => {}} />)

    expect(
      (screen.getByRole('checkbox', { name: 'On site' }) as HTMLInputElement).checked,
    ).toBe(true)
    expect(
      (screen.getByRole('checkbox', { name: 'Planning' }) as HTMLInputElement).checked,
    ).toBe(false)
  })

  it('adds a status when it is ticked', () => {
    const onChange = vi.fn()
    render(<StatusFilter selected={['Planning']} onChange={onChange} />)

    fireEvent.click(screen.getByRole('checkbox', { name: 'Complete' }))

    expect(onChange).toHaveBeenCalledWith(['Planning', 'Complete'])
  })

  it('removes a status when it is unticked', () => {
    const onChange = vi.fn()
    render(<StatusFilter selected={['Planning', 'Complete']} onChange={onChange} />)

    fireEvent.click(screen.getByRole('checkbox', { name: 'Planning' }))

    expect(onChange).toHaveBeenCalledWith(['Complete'])
  })

  it.each([
    [[], null],
    [['Planning'], 'Showing 1 of 4 statuses'],
    [['Planning', 'OnSite'], 'Showing 2 of 4 statuses'],
    [['Appraisal', 'Planning', 'OnSite', 'Complete'], null],
  ] as const)('with %j the note says %s', (selected, note) => {
    render(<StatusFilter selected={[...selected]} onChange={() => {}} />)

    expect(screen.queryByText(/^Showing/)?.textContent ?? null).toBe(note)
  })
})
