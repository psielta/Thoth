import { UserRound } from 'lucide-react'
import type { WorkflowActor } from '@/api/schemas'
import { ACTOR_LABELS } from './constants'

export function PhaseBadge({ name, color }: { name: string; color?: string | null }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-md bg-muted px-2 py-1 text-xs font-medium text-foreground">
      <span className="h-2 w-2 shrink-0 rounded-full" style={{ backgroundColor: color ?? '#ffb900' }} />
      <span className="truncate">{name}</span>
    </span>
  )
}

export function ActorBadge({ actor, highlight }: { actor: WorkflowActor; highlight?: boolean }) {
  const isHuman = actor === 'Human'
  const tone = highlight && isHuman
    ? 'bg-warning-soft text-warning-foreground'
    : actor === 'ClaudeCode'
      ? 'bg-info-soft text-info-foreground'
      : actor === 'Codex'
        ? 'bg-success-soft text-success-foreground'
        : 'bg-muted text-muted-foreground'

  return (
    <span className={`inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs font-medium ${tone}`}>
      <UserRound className="h-3 w-3" />
      {ACTOR_LABELS[actor]}
    </span>
  )
}
