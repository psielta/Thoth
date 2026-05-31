import { useQuery } from '@tanstack/react-query'
import { Clock3, GitCompare, Loader2, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'
import remarkGfm from 'remark-gfm'
import { listPromptVersions } from '@/api/prompts'
import { queryKeys } from '@/api/query-keys'
import type { PromptVersion } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { DiffViewerModal } from '@/features/diff/diff-viewer-modal'
import { cn } from '@/lib/utils'
import { AGENT_LABELS, KIND_LABELS, STATUS_LABELS } from './constants'

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

// Thin wrapper uses key to auto-reset all state when promptId changes.
export function PromptVersions({ promptId }: { promptId: string }) {
  return <PromptVersionsPanel key={promptId} promptId={promptId} />
}

function PromptVersionsPanel({ promptId }: { promptId: string }) {
  const [selectedVersion, setSelectedVersion] = useState<PromptVersion | null>(null)
  const [compareIds, setCompareIds] = useState<string[]>([])
  const [isCompareOpen, setIsCompareOpen] = useState(false)

  const versionsQuery = useQuery({
    queryKey: queryKeys.prompts.versions(promptId),
    queryFn: () => listPromptVersions(promptId),
  })

  // Derive valid IDs without a useEffect — stale IDs are simply excluded.
  const validCompareIds = useMemo(() => {
    if (!versionsQuery.data) return compareIds
    const ids = new Set(versionsQuery.data.map((v) => v.id))
    return compareIds.filter((id) => ids.has(id))
  }, [compareIds, versionsQuery.data])

  const compareVersions = useMemo(() => {
    if (validCompareIds.length !== 2 || !versionsQuery.data) return null
    const versions = validCompareIds
      .map((id) => versionsQuery.data!.find((v) => v.id === id))
      .filter((v): v is PromptVersion => v !== undefined)
    if (versions.length !== 2) return null
    return versions.sort((a, b) => a.versionNumber - b.versionNumber)
  }, [validCompareIds, versionsQuery.data])

  const toggleCompare = (id: string) => {
    setCompareIds((prev) => {
      if (prev.includes(id)) return prev.filter((x) => x !== id)
      if (prev.length >= 2) return prev
      return [...prev, id]
    })
  }

  return (
    <aside className="grid content-start gap-3 rounded-lg border border-[#d9dfd5] bg-white p-4">
      <div className="flex items-center gap-2 text-sm font-semibold text-[#172126]">
        <Clock3 className="h-4 w-4 text-[#5e7461]" />
        Versoes
      </div>

      {validCompareIds.length > 0 && (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            disabled={validCompareIds.length !== 2}
            onClick={() => setIsCompareOpen(true)}
          >
            <GitCompare className="h-4 w-4" />
            Comparar versoes
          </Button>
          <button
            type="button"
            className="text-xs text-[#66746b] underline"
            onClick={() => setCompareIds([])}
          >
            Limpar
          </button>
        </div>
      )}

      {versionsQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-[#66746b]">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando
        </div>
      ) : null}

      <div className="grid gap-2">
        {versionsQuery.data?.map((version) => {
          const isChecked = validCompareIds.includes(version.id)
          const isDisabled = validCompareIds.length >= 2 && !isChecked
          return (
            <div key={version.id} className="flex items-start gap-2">
              <input
                type="checkbox"
                id={`compare-${version.id}`}
                className="mt-3 h-4 w-4 shrink-0 accent-[#254632]"
                checked={isChecked}
                onChange={() => toggleCompare(version.id)}
                disabled={isDisabled}
                aria-label={`Selecionar v${version.versionNumber} para comparacao`}
              />
              <button
                type="button"
                className={cn(
                  'grid min-w-0 flex-1 gap-1 rounded-md border p-3 text-left transition-colors',
                  selectedVersion?.id === version.id
                    ? 'border-[#254632] bg-[#eef2eb]'
                    : 'border-[#d9dfd5] bg-white hover:bg-[#f7f8f6]',
                )}
                onClick={() => setSelectedVersion(version)}
              >
                <div className="text-sm font-medium text-[#172126]">v{version.versionNumber}</div>
                <div className="mt-1 text-xs text-[#66746b]">
                  {dateFormatter.format(new Date(version.createdAtUtc))}
                </div>
                <div className="truncate text-xs text-[#66746b]">{version.title}</div>
              </button>
            </div>
          )
        })}
      </div>

      {selectedVersion ? (
        <PromptVersionPreview version={selectedVersion} onClose={() => setSelectedVersion(null)} />
      ) : null}

      {isCompareOpen && compareVersions ? (
        <DiffViewerModal
          oldContent={compareVersions[0].content}
          newContent={compareVersions[1].content}
          oldLabel={`v${compareVersions[0].versionNumber}`}
          newLabel={`v${compareVersions[1].versionNumber}`}
          onClose={() => setIsCompareOpen(false)}
        />
      ) : null}
    </aside>
  )
}

function PromptVersionPreview({
  version,
  onClose,
}: {
  version: PromptVersion
  onClose: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 grid bg-[#172126]/35 p-4 backdrop-blur-sm sm:p-6" role="dialog" aria-modal="true">
      <div className="mx-auto grid h-full w-full max-w-5xl content-start overflow-hidden rounded-lg border border-[#d9dfd5] bg-white shadow-2xl">
        <div className="flex min-w-0 items-start justify-between gap-3 border-b border-[#d9dfd5] p-4">
          <div className="min-w-0">
            <div className="flex min-w-0 flex-wrap items-center gap-2">
              <h2 className="truncate text-base font-semibold text-[#172126]">v{version.versionNumber}</h2>
              <Badge variant="blue">{AGENT_LABELS[version.targetAgent]}</Badge>
              <Badge variant="neutral">{KIND_LABELS[version.kind]}</Badge>
              <Badge variant="green">{STATUS_LABELS[version.status]}</Badge>
            </div>
            <p className="mt-1 truncate text-sm font-medium text-[#172126]">{version.title}</p>
            <p className="mt-1 text-xs text-[#66746b]">{dateFormatter.format(new Date(version.createdAtUtc))}</p>
          </div>

          <Button type="button" variant="ghost" size="icon" onClick={onClose} aria-label="Fechar">
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="min-h-0 overflow-auto p-4">
          <div className="linked-markdown">
            <ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]}>
              {version.content}
            </ReactMarkdown>
          </div>
        </div>
      </div>
    </div>
  )
}
