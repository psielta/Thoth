import { CircleDot } from 'lucide-react'
import type { WorkflowEvent } from '@/api/schemas'
import { ACTOR_LABELS, EVENT_TYPE_LABELS, formatDateTime, formatRelativeTime } from './constants'

export function WorkflowTimeline({ events }: { events: WorkflowEvent[] }) {
  if (events.length === 0) {
    return <p className="text-sm text-muted-foreground">Sem eventos ainda.</p>
  }

  const ordered = [...events].sort((a, b) => b.occurredAtUtc.localeCompare(a.occurredAtUtc))

  return (
    <ol className="grid gap-3">
      {ordered.map((event) => (
        <li key={event.id} className="grid gap-1 border-l-2 border-border pl-3">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm">
            <CircleDot className="h-3.5 w-3.5 shrink-0 text-ring" />
            <span className="font-medium text-foreground">{EVENT_TYPE_LABELS[event.type]}</span>
            {event.phaseName ? <span className="text-ring">· {event.phaseName}</span> : null}
            {event.actor ? <span className="text-muted-foreground">· {ACTOR_LABELS[event.actor]}</span> : null}
          </div>
          {event.note ? <p className="whitespace-pre-wrap text-sm text-foreground">{event.note}</p> : null}
          <span className="text-xs text-subtle-foreground" title={formatDateTime(event.occurredAtUtc)}>
            {formatDateTime(event.occurredAtUtc)} · {formatRelativeTime(event.occurredAtUtc)}
          </span>
        </li>
      ))}
    </ol>
  )
}
