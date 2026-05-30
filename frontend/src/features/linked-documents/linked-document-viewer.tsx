import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, FileText, Loader2, Pause, Play, RefreshCw, Trash2 } from 'lucide-react'
import type { ComponentProps } from 'react'
import { useState } from 'react'
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

export function LinkedDocumentViewer({ documentId, initialDocument, onRemoved }: LinkedDocumentViewerProps) {
  const queryClient = useQueryClient()
  const [selectedVersion, setSelectedVersion] = useState<number | undefined>()

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

  const isBusy =
    refreshMutation.isPending || pauseMutation.isPending || resumeMutation.isPending || removeMutation.isPending

  if (!document) {
    return (
      <div className="flex min-h-[22rem] items-center justify-center rounded-lg border border-[#d9dfd5] bg-white text-sm text-[#66746b]">
        Selecione um markdown vinculado.
      </div>
    )
  }

  return (
    <section className="grid min-w-0 gap-4 xl:grid-cols-[minmax(0,1fr)_16rem]">
      <div className="grid min-w-0 gap-4 rounded-lg border border-[#d9dfd5] bg-white">
        <div className="grid min-w-0 gap-3 border-b border-[#d9dfd5] p-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
            <div className="min-w-0">
              <div className="flex min-w-0 items-center gap-2">
                <FileText className="h-4 w-4 shrink-0 text-[#5e7461]" />
                <h2 className="truncate text-base font-semibold text-[#172126]">{document.displayName}</h2>
                <Badge variant={statusVariants[document.status]}>{statusLabels[document.status]}</Badge>
              </div>
              <p className="mt-1 truncate text-xs text-[#66746b]" title={document.absolutePath}>
                {document.absolutePath}
              </p>
              <p className="mt-1 text-xs text-[#66746b]">
                v{document.currentVersion || 1}
                {document.lastSyncedAtUtc
                  ? ` atualizado em ${dateFormatter.format(new Date(document.lastSyncedAtUtc))}`
                  : ''}
              </p>
            </div>

            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="secondary"
                size="sm"
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
            <div className="flex items-start gap-2 rounded-md border border-[#f8b4aa] bg-[#fff3f0] p-3 text-sm text-[#8a241b]">
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
              <span className="min-w-0">{document.lastError}</span>
            </div>
          ) : null}
        </div>

        <div className="min-h-[36rem] min-w-0 p-4">
          {contentQuery.isLoading ? (
            <div className="flex items-center gap-2 text-sm text-[#66746b]">
              <Loader2 className="h-4 w-4 animate-spin" />
              Carregando markdown
            </div>
          ) : contentQuery.error ? (
            <div className="rounded-md border border-[#f8b4aa] bg-[#fff3f0] p-3 text-sm text-[#8a241b]">
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
      />
    </section>
  )
}
