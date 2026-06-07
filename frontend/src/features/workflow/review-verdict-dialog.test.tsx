import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Workflow } from '@/api/schemas'
import * as workflowApi from '@/api/workflow'
import { ReviewVerdictDialog } from './review-verdict-dialog'

vi.mock('@/api/workflow')
vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }))

const reviewWorkflow: Workflow = {
  id: 'w1',
  promptId: 'p1',
  status: 'Active',
  currentPhaseId: 'ph-review',
  currentPhaseName: 'Revisão do plano',
  currentPhaseColor: '#7c3aed',
  currentActor: 'Codex',
  startedAtUtc: '2026-06-01T12:00:00Z',
  enteredCurrentPhaseAtUtc: '2026-06-01T12:00:00Z',
  currentPhaseIteration: 1,
  reviewVerdictSourcePhaseName: null,
  updatedAtUtc: '2026-06-01T12:00:00Z',
  rowVersion: '7',
  phases: [
    { id: 'ph-review', name: 'Revisão do plano', defaultActor: 'Codex', orderIndex: 0, color: '#7c3aed', role: 'PlanReview' },
    { id: 'ph-fix', name: 'Correção do plano', defaultActor: 'ClaudeCode', orderIndex: 1, color: '#d97706', role: 'PlanCorrection' },
  ],
  events: [],
}

function renderDialog() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  const onClose = vi.fn()
  render(
    <QueryClientProvider client={client}>
      <ReviewVerdictDialog promptId="p1" onClose={onClose} />
    </QueryClientProvider>,
  )
  return { onClose }
}

describe('ReviewVerdictDialog', () => {
  beforeEach(() => {
    vi.mocked(workflowApi.getWorkflow).mockResolvedValue(reviewWorkflow)
    vi.mocked(workflowApi.addReviewVerdict).mockResolvedValue({
      ...reviewWorkflow,
      currentPhaseId: 'ph-fix',
      currentPhaseName: 'Correção do plano',
      currentActor: 'ClaudeCode',
      reviewVerdictSourcePhaseName: 'Revisão do plano',
      rowVersion: '8',
    })
  })

  afterEach(() => {
    cleanup()
    vi.clearAllMocks()
  })

  it('submits the trimmed verdict with the current row version and closes', async () => {
    const { onClose } = renderDialog()

    const field = await screen.findByLabelText('Veredito do agente')
    fireEvent.change(field, { target: { value: '  Há 3 pontos a corrigir  ' } })
    fireEvent.click(screen.getByRole('button', { name: /registrar e avançar/i }))

    await waitFor(() =>
      expect(vi.mocked(workflowApi.addReviewVerdict)).toHaveBeenCalledWith('p1', 'Há 3 pontos a corrigir', '7'),
    )
    await waitFor(() => expect(onClose).toHaveBeenCalledTimes(1))
  })

  it('keeps the submit button disabled until a verdict is typed', async () => {
    renderDialog()

    await screen.findByLabelText('Veredito do agente')
    expect(screen.getByRole('button', { name: /registrar e avançar/i })).toBeDisabled()
  })
})
