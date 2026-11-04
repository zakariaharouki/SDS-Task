import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { SchemeStatus } from '../types/scheme'
import StatusBadge from './StatusBadge'

const statuses: [SchemeStatus, string][] = [
  ['Appraisal', 'Appraisal'],
  ['Planning', 'Planning'],
  ['OnSite', 'On site'],
  ['Complete', 'Complete'],
]

// WCAG contrast of white text against an rgb(...) background.
const contrastWithWhite = (background: string) => {
  const [red, green, blue] = (background.match(/\d+/g) ?? []).map((part) => {
    const channel = Number(part) / 255
    return channel <= 0.03928
      ? channel / 12.92
      : ((channel + 0.055) / 1.055) ** 2.4
  })
  const luminance = 0.2126 * red + 0.7152 * green + 0.0722 * blue
  return 1.05 / (luminance + 0.05)
}

describe('StatusBadge', () => {
  it.each(statuses)('%s shows as "%s"', (status, label) => {
    render(<StatusBadge status={status} />)

    expect(screen.getByText(label)).toBeTruthy()
  })

  it.each(statuses)('%s has readable white text', (status, label) => {
    render(<StatusBadge status={status} />)

    const { backgroundColor } = screen.getByText(label).style

    expect(backgroundColor).toMatch(/^rgb/)
    expect(contrastWithWhite(backgroundColor)).toBeGreaterThanOrEqual(4.5)
  })
})
