import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import type { WorkflowEvent } from '@/api/schemas'
import { WorkflowTimeline } from './workflow-timeline'

const events: WorkflowEvent[] = [
  {
    id: 'a',
    type: 'WorkflowStarted',
    phaseId: 'p1',
    phaseName: 'Planejamento',
    actor: 'ClaudeCode',
    note: null,
    occurredAtUtc: '2026-06-01T12:00:00Z',
  },
  {
    id: 'b',
    type: 'PhaseChanged',
    phaseId: 'p2',
    phaseName: 'Revisão do plano',
    actor: 'Codex',
    note: null,
    occurredAtUtc: '2026-06-01T13:00:00Z',
  },
  {
    id: 'c',
    type: 'Note',
    phaseId: 'p2',
    phaseName: 'Revisão do plano',
    actor: 'Codex',
    note: 'Ajustar contrato da API',
    occurredAtUtc: '2026-06-01T13:30:00Z',
  },
]

describe('WorkflowTimeline', () => {
  it('renders events newest first with labels and notes', () => {
    render(<WorkflowTimeline events={events} />)

    expect(screen.getByText('Fluxo iniciado')).toBeTruthy()
    expect(screen.getByText('Mudou de fase')).toBeTruthy()
    expect(screen.getByText('Ajustar contrato da API')).toBeTruthy()

    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(3)
    expect(items[0].textContent).toContain('Nota')
  })

  it('shows an empty state', () => {
    render(<WorkflowTimeline events={[]} />)
    expect(screen.getByText('Sem eventos ainda.')).toBeTruthy()
  })
})
