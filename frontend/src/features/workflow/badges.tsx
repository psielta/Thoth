import { UserRound } from 'lucide-react'
import type { WorkflowActor } from '@/api/schemas'
import { ACTOR_LABELS } from './constants'

export function PhaseBadge({ name, color }: { name: string; color?: string | null }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-md bg-[#eef2eb] px-2 py-1 text-xs font-medium text-[#2c3a31]">
      <span className="h-2 w-2 shrink-0 rounded-full" style={{ backgroundColor: color ?? '#5e7461' }} />
      <span className="truncate">{name}</span>
    </span>
  )
}

export function ActorBadge({ actor, highlight }: { actor: WorkflowActor; highlight?: boolean }) {
  const isHuman = actor === 'Human'
  const tone = highlight && isHuman
    ? 'bg-[#fff0c2] text-[#6b4d00]'
    : actor === 'ClaudeCode'
      ? 'bg-[#e0eefb] text-[#234c71]'
      : actor === 'Codex'
        ? 'bg-[#e1f2e1] text-[#215631]'
        : 'bg-[#eef2eb] text-[#425048]'

  return (
    <span className={`inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs font-medium ${tone}`}>
      <UserRound className="h-3 w-3" />
      {ACTOR_LABELS[actor]}
    </span>
  )
}
