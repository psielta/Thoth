import { Link } from '@tanstack/react-router'
import { ExternalLink, X } from 'lucide-react'
import { useState } from 'react'

import { Button } from '@/components/ui/button'
import type { DetailTab } from '@/features/prompts/prompt-detail-search'
import { PromptDetailView } from '@/features/prompts/prompt-detail'
import { OpenVsCodeButton } from '@/features/workspaces/open-vscode-button'

type PromptDetailDrawerProps = {
  workspaceId: string
  promptId: string
  title?: string
  onClose: () => void
}

export function PromptDetailDrawer({ workspaceId, promptId, title, onClose }: PromptDetailDrawerProps) {
  const [tab, setTab] = useState<DetailTab>('prompt')

  return (
    <div
      className="fixed inset-0 z-50 flex justify-end bg-black/50 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="prompt-detail-drawer-title"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div className="grid h-full w-full max-w-[min(96vw,72rem)] grid-rows-[auto_minmax(0,1fr)] border-l border-border bg-card shadow-2xl">
        <div className="flex min-w-0 items-center justify-between gap-2 border-b border-border px-4 py-2.5">
          <h2 id="prompt-detail-drawer-title" className="min-w-0 truncate text-base font-semibold text-foreground">
            {title || 'Detalhes do prompt'}
          </h2>

          <div className="flex shrink-0 items-center gap-1">
            <OpenVsCodeButton
              workingDirectoryId={workspaceId}
              iconOnly
              variant="ghost"
              className="text-muted-foreground"
            />
            <Link
              to="/workspaces/$workspaceId/prompts/$promptId"
              params={{ workspaceId, promptId }}
              search={{}}
              className="inline-flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-muted"
              aria-label="Abrir na pagina completa de edicao"
              title="Abrir na pagina completa de edicao"
            >
              <ExternalLink className="h-4 w-4" />
            </Link>

            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-muted-foreground"
              onClick={onClose}
              aria-label="Fechar"
              title="Fechar"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <div className="min-h-0 overflow-auto px-4 py-3">
          <PromptDetailView
            workspaceId={workspaceId}
            promptId={promptId}
            activeTab={tab}
            onTabChange={setTab}
            onDeleted={onClose}
          />
        </div>
      </div>
    </div>
  )
}
