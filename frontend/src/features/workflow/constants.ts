import type {
  PromptWorkflowStatus,
  WorkflowActor,
  WorkflowEventType,
  WorkflowPhase,
  WorkflowPhaseRole,
} from '@/api/schemas'

export const ACTOR_LABELS: Record<WorkflowActor, string> = {
  ClaudeCode: 'Claude',
  Codex: 'Codex',
  Human: 'Você',
}

export const ACTOR_OPTIONS = [
  { value: 'ClaudeCode', label: ACTOR_LABELS.ClaudeCode },
  { value: 'Codex', label: ACTOR_LABELS.Codex },
  { value: 'Human', label: ACTOR_LABELS.Human },
] satisfies Array<{ value: WorkflowActor; label: string }>

export const WORKFLOW_STATUS_LABELS: Record<PromptWorkflowStatus, string> = {
  Active: 'Em andamento',
  Done: 'Concluída',
}

export const EVENT_TYPE_LABELS: Record<WorkflowEventType, string> = {
  WorkflowStarted: 'Fluxo iniciado',
  PhaseChanged: 'Mudou de fase',
  ActorChanged: 'Trocou responsável',
  Note: 'Nota',
  Completed: 'Concluída',
  Reopened: 'Reaberta',
  PhasesEdited: 'Fases editadas',
}

// Fases de revisão onde o botão "Adicionar nota de revisão" aparece, e a fase de correção
// correspondente (apenas para o texto do diálogo; o backend é a fonte da verdade do destino).
export const REVIEW_TARGET_LABEL = {
  PlanReview: 'Correção do plano',
  CodeReview: 'Correção da revisão',
} satisfies Partial<Record<WorkflowPhaseRole, string>>

export function isReviewPhaseRole(role: WorkflowPhaseRole | null | undefined): role is 'PlanReview' | 'CodeReview' {
  return role === 'PlanReview' || role === 'CodeReview'
}

export function currentPhaseRole(
  phases: WorkflowPhase[],
  currentPhaseId: string | null | undefined,
): WorkflowPhaseRole | null {
  if (!currentPhaseId) {
    return null
  }

  return phases.find((phase) => phase.id === currentPhaseId)?.role ?? null
}

export const PHASE_COLOR_PALETTE = [
  '#2563eb',
  '#7c3aed',
  '#d97706',
  '#0d9488',
  '#0891b2',
  '#db2777',
  '#16a34a',
  '#15803d',
  '#9333ea',
  '#dc2626',
]

const RELATIVE_UNITS: Array<[Intl.RelativeTimeFormatUnit, number]> = [
  ['year', 31_536_000_000],
  ['month', 2_592_000_000],
  ['day', 86_400_000],
  ['hour', 3_600_000],
  ['minute', 60_000],
  ['second', 1_000],
]

export function formatRelativeTime(iso: string): string {
  const target = new Date(iso).getTime()
  if (Number.isNaN(target)) {
    return ''
  }

  const diffMs = target - Date.now()
  const abs = Math.abs(diffMs)
  const formatter = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' })

  for (const [unit, ms] of RELATIVE_UNITS) {
    if (abs >= ms || unit === 'second') {
      return formatter.format(Math.round(diffMs / ms), unit)
    }
  }

  return ''
}

export function formatDateTime(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}
