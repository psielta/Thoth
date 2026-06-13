import { Terminal as TerminalIcon } from 'lucide-react'
import { useEffect, useRef } from 'react'
import { cn } from '@/lib/utils'
import { resolveTerminalTabLabel, type TerminalTabPreference } from './terminal-tab-preferences'
import { TERMINAL_SWITCHER_CYCLE_LABEL, TERMINAL_SWITCHER_OPEN_LABEL } from './terminal-switcher-shortcuts'

export type TerminalSwitcherItem = {
  sessionId: string
  index: number
  preference?: TerminalTabPreference
}

type TerminalSwitcherProps = {
  items: TerminalSwitcherItem[]
  highlightedSessionId: string
}

export function TerminalSwitcher({ items, highlightedSessionId }: TerminalSwitcherProps) {
  const listRef = useRef<HTMLUListElement>(null)

  useEffect(() => {
    const highlightedIndex = items.findIndex((item) => item.sessionId === highlightedSessionId)
    const active = listRef.current?.querySelector<HTMLElement>(`[data-index="${highlightedIndex}"]`)
    active?.scrollIntoView?.({ block: 'nearest' })
  }, [highlightedSessionId, items])

  return (
    <div
      className="fixed inset-0 z-[70] flex items-start justify-center bg-black/45 p-4 pt-[14vh] backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-label="Alternar terminal"
    >
      <div className="w-full max-w-md overflow-hidden rounded-lg border border-border bg-card shadow-2xl">
        <div className="border-b border-border px-4 py-3">
          <p className="text-sm font-medium text-foreground">Alternar terminal</p>
          <p className="text-xs text-muted-foreground">
            {TERMINAL_SWITCHER_OPEN_LABEL} abre este painel. Use Tab ou setas para navegar, Enter para abrir e Esc
            para cancelar. {TERMINAL_SWITCHER_CYCLE_LABEL} alterna direto.
          </p>
        </div>

        <ul ref={listRef} className="max-h-[40vh] overflow-y-auto p-1">
          {items.map((item, index) => {
            const label = resolveTerminalTabLabel(item.preference, item.index)
            const isHighlighted = item.sessionId === highlightedSessionId
            const accentColor = item.preference?.color

            return (
              <li
                key={item.sessionId}
                data-index={index}
                className={cn(
                  'flex items-center gap-2 rounded-md px-3 py-2 text-sm',
                  isHighlighted ? 'bg-primary text-primary-foreground' : 'text-foreground',
                )}
                style={
                  accentColor && !isHighlighted
                    ? { boxShadow: `inset 4px 0 0 0 ${accentColor}` }
                    : undefined
                }
              >
                <TerminalIcon className="h-4 w-4 shrink-0 opacity-80" />
                <span className="truncate">{label}</span>
              </li>
            )
          })}
        </ul>
      </div>
    </div>
  )
}