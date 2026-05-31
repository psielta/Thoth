import { CircleDot } from 'lucide-react'
import type { WorkflowEvent } from '@/api/schemas'
import { ACTOR_LABELS, EVENT_TYPE_LABELS, formatDateTime, formatRelativeTime } from './constants'

export function WorkflowTimeline({ events }: { events: WorkflowEvent[] }) {
  if (events.length === 0) {
    return <p className="text-sm text-[#66746b]">Sem eventos ainda.</p>
  }

  const ordered = [...events].sort((a, b) => b.occurredAtUtc.localeCompare(a.occurredAtUtc))

  return (
    <ol className="grid gap-3">
      {ordered.map((event) => (
        <li key={event.id} className="grid gap-1 border-l-2 border-[#d9dfd5] pl-3">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm">
            <CircleDot className="h-3.5 w-3.5 shrink-0 text-[#5e7461]" />
            <span className="font-medium text-[#172126]">{EVENT_TYPE_LABELS[event.type]}</span>
            {event.phaseName ? <span className="text-[#5e7461]">· {event.phaseName}</span> : null}
            {event.actor ? <span className="text-[#66746b]">· {ACTOR_LABELS[event.actor]}</span> : null}
          </div>
          {event.note ? <p className="whitespace-pre-wrap text-sm text-[#3a464d]">{event.note}</p> : null}
          <span className="text-xs text-[#8a958c]" title={formatDateTime(event.occurredAtUtc)}>
            {formatDateTime(event.occurredAtUtc)} · {formatRelativeTime(event.occurredAtUtc)}
          </span>
        </li>
      ))}
    </ol>
  )
}
