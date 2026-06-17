import { Terminal as TerminalIcon, X, ZoomIn, ZoomOut } from 'lucide-react'
import type { TerminalSession } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { TerminalFrame } from '@/features/prompts/terminal-frame'
import { TerminalView } from '@/features/prompts/terminal-view'

type TerminalViewDrawerProps = {
  session: TerminalSession
  label: string
  fontSize: number
  onClose: () => void
  onSessionExit: (sessionId: string, exitCode: number) => void
  onAdjustFontSize: (delta: number) => void
}

/**
 * Right-side drawer que hospeda um unico terminal aberto a partir de /terminais.
 * Tira o terminal da lista rolavel da pagina (acaba a "rolagem dentro de rolagem")
 * e da altura/largura cheias ao xterm. Espelha o padrao de agent-terminal-drawer.tsx.
 * Fecha pelo botao do cabecalho ou clique no backdrop; Escape nao e vinculado de
 * proposito para poder ser usado dentro do terminal (vim, etc.).
 */
export function TerminalViewDrawer({
  session,
  label,
  fontSize,
  onClose,
  onSessionExit,
  onAdjustFontSize,
}: TerminalViewDrawerProps) {
  const shellName = session.shell.split(/[\\/]/).pop() ?? session.shell
  const childTitle = session.ownerPromptTitle ?? null

  return (
    <div
      className="fixed inset-0 z-50 flex justify-end bg-black/50 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="terminal-view-drawer-title"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div className="grid h-full w-full max-w-[min(96vw,72rem)] grid-rows-[auto_minmax(0,1fr)] border-l border-border bg-card shadow-2xl">
        <div className="flex min-w-0 items-center gap-2 border-b border-border px-4 py-2.5">
          <TerminalIcon className="h-4 w-4 shrink-0 text-muted-foreground" />
          <h2
            id="terminal-view-drawer-title"
            className="min-w-0 flex-1 truncate text-base font-semibold text-foreground"
            title={label}
          >
            {label}
          </h2>

          {session.isChild ? (
            <Badge
              variant="blue"
              className="hidden max-w-[12rem] shrink-0 sm:inline-flex"
              title={childTitle ? `Filho: ${childTitle}` : 'Terminal de prompt filho'}
            >
              <span className="truncate">{childTitle ? `Filho: ${childTitle}` : 'Filho'}</span>
            </Badge>
          ) : null}

          <Badge variant="neutral" className="shrink-0 font-mono" title={session.shell}>
            {shellName}
          </Badge>

          <div
            role="group"
            aria-label="Zoom do terminal"
            className="flex shrink-0 items-center gap-0.5 rounded-md border border-border bg-card p-0.5"
          >
            <Button
              type="button"
              size="icon"
              variant="ghost"
              className="h-7 w-7"
              title="Diminuir fonte"
              aria-label="Diminuir fonte do terminal"
              onClick={() => onAdjustFontSize(-1)}
            >
              <ZoomOut className="h-3.5 w-3.5" />
            </Button>
            <span className="px-1 font-mono text-[0.65rem] tabular-nums text-muted-foreground">
              {fontSize}px
            </span>
            <Button
              type="button"
              size="icon"
              variant="ghost"
              className="h-7 w-7"
              title="Aumentar fonte (Ctrl+scroll no terminal tambem aplica zoom)"
              aria-label="Aumentar fonte do terminal"
              onClick={() => onAdjustFontSize(1)}
            >
              <ZoomIn className="h-3.5 w-3.5" />
            </Button>
          </div>

          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-8 w-8 shrink-0 text-muted-foreground"
            onClick={onClose}
            aria-label="Fechar"
            title="Fechar"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="h-full min-h-0 px-4 py-3">
          <TerminalFrame variant="drawer">
            <TerminalView
              sessionId={session.id}
              active
              fontSize={fontSize}
              onZoom={onAdjustFontSize}
              onSessionExit={onSessionExit}
            />
          </TerminalFrame>
        </div>
      </div>
    </div>
  )
}
