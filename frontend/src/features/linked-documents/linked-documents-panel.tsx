import { useQuery } from '@tanstack/react-query'
import { Loader2, Radio } from 'lucide-react'
import { listLinkedDocuments } from '@/api/linked-documents'
import { queryKeys } from '@/api/query-keys'
import { usePromptHub } from '@/realtime/prompt-hub'
import { LinkDocumentForm } from './link-document-form'
import { LinkedDocumentViewer } from './linked-document-viewer'

type LinkedDocumentsPanelProps = {
  promptId: string
}

function RealtimeStatus({ connected }: { connected: boolean }) {
  return (
    <div className="flex shrink-0 items-center gap-1.5 rounded-md border border-border px-2 py-1 text-xs text-muted-foreground">
      <Radio className={connected ? 'h-3.5 w-3.5 text-success-foreground' : 'h-3.5 w-3.5 text-destructive'} />
      {connected ? 'Online' : 'Offline'}
    </div>
  )
}

export function LinkedDocumentsPanel({ promptId }: LinkedDocumentsPanelProps) {
  const hub = usePromptHub()

  const documentsQuery = useQuery({
    queryKey: queryKeys.linkedDocuments.forPrompt(promptId),
    queryFn: () => listLinkedDocuments(promptId),
  })

  const document = documentsQuery.data?.[0]

  if (documentsQuery.isLoading) {
    return (
      <div className="flex min-h-[28rem] items-center justify-center rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Carregando plano vinculado
      </div>
    )
  }

  if (!document) {
    return (
      <div className="flex min-h-[28rem] items-center justify-center rounded-lg border border-border bg-card p-6">
        <div className="grid w-full max-w-lg gap-4">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h2 className="text-base font-semibold text-foreground">Plano vinculado</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                Vincule um markdown externo para renderizar o plano e acompanhar novas versoes em tempo real.
              </p>
            </div>
            <RealtimeStatus connected={hub.connected} />
          </div>
          <LinkDocumentForm promptId={promptId} />
        </div>
      </div>
    )
  }

  return (
    <div className="grid min-w-0 gap-2">
      <div className="flex justify-end">
        <RealtimeStatus connected={hub.connected} />
      </div>
      <LinkedDocumentViewer documentId={document.id} initialDocument={document} onRemoved={() => {}} />
    </div>
  )
}
