import { cleanup, render, screen } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { TerminalCard } from './terminal-card'

vi.mock('@tanstack/react-router', () => ({
  Link: ({
    children,
    className,
  }: {
    children?: ReactNode
    className?: string
  }) => <a className={className}>{children}</a>,
}))

const session = {
  id: '33333333-3333-4333-8333-333333333333',
  promptId: '11111111-1111-4111-8111-111111111111',
  shell: 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe',
  cwd: 'D:/globalleitorpdf/globalleitorpdf',
  createdAtUtc: '2026-06-18T11:34:00Z',
  isChild: true,
  ownerPromptTitle:
    'Revisar plano com prompt pai: em-contdoclicita-contdoclicita-views-sol-sequential-eagle.md',
}

describe('TerminalCard', () => {
  afterEach(cleanup)

  it('keeps long child prompt labels constrained inside the card', () => {
    render(
      <TerminalCard
        session={session}
        index={0}
        workspaceId="22222222-2222-4222-8222-222222222222"
        linkPromptId="11111111-1111-4111-8111-111111111111"
        closeDisabled={false}
        onView={vi.fn()}
        onClose={vi.fn()}
      />,
    )

    const childLabel = screen.getByText(/^Filho: Revisar plano/)
    const badge = childLabel.parentElement
    const card = badge?.closest('div')

    expect(card?.className).toContain('grid-cols-[minmax(0,1fr)]')
    expect(badge).toHaveClass('min-w-0')
    expect(badge).toHaveClass('max-w-full')
    expect(badge).toHaveClass('overflow-hidden')
    expect(childLabel).toHaveClass('min-w-0')
    expect(childLabel).toHaveClass('truncate')
  })
})
