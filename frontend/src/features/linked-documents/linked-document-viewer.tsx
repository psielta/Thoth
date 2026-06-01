import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, FileText, Loader2, Pause, Play, RefreshCw, Trash2 } from 'lucide-react'
import type { ComponentProps } from 'react'
import { useMemo, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'
import remarkGfm from 'remark-gfm'
import { toast } from 'sonner'
import {
  getLinkedDocument,
  getLinkedDocumentContent,
  listLinkedDocumentVersions,
  pauseLinkedDocument,
  refreshLinkedDocument,
  removeLinkedDocument,
  resumeLinkedDocument,
} from '@/api/linked-documents'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { LinkedDocument } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { DiffViewerModal } from '@/features/diff/diff-viewer-modal'
import { useLinkedPlanCompare } from '@/features/diff/use-linked-plan-compare'
import { GeneratePromptMenu } from './generate-prompt-menu'
import { LinkedDocumentHistory } from './linked-document-history'

type LinkedDocumentViewerProps = {
  documentId: string
  initialDocument?: LinkedDocument
  onRemoved: () => void
}

const statusLabels: Record<LinkedDocument['status'], string> = {
  Draft: 'Rascunho',
  Tracking: 'Monitorando',
  Paused: 'Pausado',
  Error: 'Erro',
  Missing: 'Nao encontrado',
}

const statusVariants: Record<LinkedDocument['status'], ComponentProps<typeof Badge>['variant']> = {
  Draft: 'neutral',
  Tracking: 'green',
  Paused: 'amber',
  Error: 'red',
  Missing: 'red',
}

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

// Thin wrapper uses key to auto-reset all state when documentId changes.
export function LinkedDocumentViewer(props: LinkedDocumentViewerProps) {
  return <LinkedDocumentViewerPanel key={props.documentId} {...props} />
}

function LinkedDocumentViewerPanel({ documentId, initialDocument, onRemoved }: LinkedDocumentViewerProps) {
  const queryClient = useQueryClient()
  const [selectedVersion, setSelectedVersion] = useState<number | undefined>()
  const [compareSelection, setCompareSelection] = useState<number[]>([])
  const [isCompareOpen, setIsCompareOpen] = useState(false)

  const documentQuery = useQuery({
    queryKey: queryKeys.linkedDocuments.detail(documentId),
    queryFn: () => getLinkedDocument(documentId),
    initialData: initialDocument,
  })

  const document = documentQuery.data
  const contentVersion = selectedVersion ?? document?.currentVersion

  const versionsQuery = useQuery({
    queryKey: queryKeys.linkedDocuments.versions(documentId),
    queryFn: () => listLinkedDocumentVersions(documentId),
  })

  // Derive valid selection without useEffect — stale numbers are simply excluded.
  const validCompareSelection = useMemo(() => {
    if (!versionsQuery.data) return compareSelection
    const nums = new Set(versionsQuery.data.map((v) => v.versionNumber))
    return compareSelection.filter((n) => nums.has(n))
  }, [compareSelection, versionsQuery.data])

  const sortedCompare = useMemo(
    () => [...validCompareSelection].sort((a, b) => a - b),
    [validCompareSelection],
  )

  const { contents, isLoading: compareLoading, error: compareError } = useLinkedPlanCompare(
    documentId,
    sortedCompare[0],
    sortedCompare[1],
    isCompareOpen,
  )

  const contentQuery = useQuery({
    queryKey: queryKeys.linkedDocuments.content(documentId, contentVersion),
    queryFn: () => getLinkedDocumentContent(documentId, contentVersion),
    enabled: Boolean(document && contentVersion),
  })

  const updateDocumentCache = async (updated: LinkedDocument, message: string) => {
    queryClient.setQueryData(queryKeys.linkedDocuments.detail(updated.id), updated)
    await queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(updated.promptId) })
    await queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.versions(updated.id) })
    await queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.contentRoot(updated.id) })
    toast.success(message)
  }

  const refreshMutation = useMutation({
    mutationFn: () => refreshLinkedDocument(documentId),
    onSuccess: (updated) => updateDocumentCache(updated, 'Markdown atualizado.'),
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const pauseMutation = useMutation({
    mutationFn: () => pauseLinkedDocument(documentId),
    onSuccess: (updated) => updateDocumentCache(updated, 'Monitoramento pausado.'),
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const resumeMutation = useMutation({
    mutationFn: () => resumeLinkedDocument(documentId),
    onSuccess: (updated) => updateDocumentCache(updated, 'Monitoramento retomado.'),
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const removeMutation = useMutation({
    mutationFn: () => removeLinkedDocument(documentId),
    onSuccess: async () => {
      if (document) {
        await queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(document.promptId) })
      }

      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.detail(documentId) })
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.contentRoot(documentId) })
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.versions(documentId) })
      onRemoved()
      toast.success('Vinculo removido.')
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const toggleCompare = (v: number) => {
    setCompareSelection((prev) => {
      if (prev.includes(v)) return prev.filter((x) => x !== v)
      if (prev.length >= 2) return prev
      return [...prev, v]
    })
  }

  const isBusy =
    refreshMutation.isPending || pauseMutation.isPending || resumeMutation.isPending || removeMutation.isPending

  if (!document) {
    return (
      <div className="flex min-h-[22rem] items-center justify-center rounded-lg border border-border bg-card text-sm text-muted-foreground">
        Selecione um markdown vinculado.
      </div>
    )
  }

  return (
    <section className="grid min-w-0 gap-4 xl:grid-cols-[minmax(0,1fr)_16rem]">
      <div className="grid min-w-0 content-start gap-4 rounded-lg border border-border bg-card">
        <div className="grid min-w-0 content-start gap-3 border-b border-border p-4">
          <div className="grid min-w-0 content-start gap-3 2xl:grid-cols-[minmax(0,1fr)_auto] 2xl:items-start">
            <div className="min-w-0">
              <div className="flex min-w-0 items-center gap-2">
                <FileText className="h-4 w-4 shrink-0 text-ring" />
                <h2 className="truncate text-base font-semibold text-foreground">{document.displayName}</h2>
                <Badge className="shrink-0" variant={statusVariants[document.status]}>
                  {statusLabels[document.status]}
                </Badge>
              </div>
              <p className="mt-1 truncate text-xs text-muted-foreground" title={document.absolutePath}>
                {document.absolutePath}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                v{document.currentVersion || 1}
                {document.lastSyncedAtUtc
                  ? ` atualizado em ${dateFormatter.format(new Date(document.lastSyncedAtUtc))}`
                  : ''}
              </p>
            </div>

            <div className="flex min-w-0 flex-wrap items-start gap-2 2xl:justify-end">
              <GeneratePromptMenu linkedDocumentId={document.id} disabled={isBusy} />

              <Button
                type="button"
                variant="secondary"
                size="sm"
                className="min-w-[6.25rem]"
                onClick={() => refreshMutation.mutate()}
                disabled={isBusy}
                title="Atualizar agora"
              >
                {refreshMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RefreshCw className="h-4 w-4" />
                )}
                Atualizar
              </Button>

              {document.status === 'Tracking' ? (
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  className="min-w-[6.25rem]"
                  onClick={() => pauseMutation.mutate()}
                  disabled={isBusy}
                >
                  {pauseMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Pause className="h-4 w-4" />}
                  Pausar
                </Button>
              ) : (
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  className="min-w-[6.25rem]"
                  onClick={() => resumeMutation.mutate()}
                  disabled={isBusy}
                >
                  {resumeMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Play className="h-4 w-4" />}
                  Retomar
                </Button>
              )}

              <Button
                type="button"
                variant="destructive"
                size="sm"
                className="min-w-[6.25rem]"
                onClick={() => {
                  if (window.confirm('Remover este vinculo de markdown?')) {
                    removeMutation.mutate()
                  }
                }}
                disabled={isBusy}
              >
                {removeMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
                Remover
              </Button>
            </div>
          </div>

          {document.lastError ? (
            <div className="flex items-start gap-2 rounded-md border border-danger-border bg-danger-soft p-3 text-sm text-danger-soft-foreground">
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
              <span className="min-w-0">{document.lastError}</span>
            </div>
          ) : null}
        </div>

        <div className="min-h-[36rem] min-w-0 p-4">
          {contentQuery.isLoading ? (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Carregando markdown
            </div>
          ) : contentQuery.error ? (
            <div className="rounded-md border border-danger-border bg-danger-soft p-3 text-sm text-danger-soft-foreground">
              {getErrorMessage(contentQuery.error)}
            </div>
          ) : (
            <div className="linked-markdown">
              <ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]}>
                {contentQuery.data?.content ?? ''}
              </ReactMarkdown>
            </div>
          )}
        </div>
      </div>

      <LinkedDocumentHistory
        currentVersion={document.currentVersion}
        selectedVersion={selectedVersion}
        versions={versionsQuery.data}
        isLoading={versionsQuery.isLoading}
        onSelectVersion={setSelectedVersion}
        compareSelection={validCompareSelection}
        onToggleCompare={toggleCompare}
        onClearCompare={() => setCompareSelection([])}
        onCompare={() => setIsCompareOpen(true)}
      />

      {isCompareOpen && sortedCompare.length === 2 ? (
        <DiffViewerModal
          oldContent={contents[0]}
          newContent={contents[1]}
          oldLabel={`v${sortedCompare[0]}`}
          newLabel={`v${sortedCompare[1]}`}
          isLoading={compareLoading}
          error={compareError ?? undefined}
          onClose={() => setIsCompareOpen(false)}
        />
      ) : null}
    </section>
  )
}
