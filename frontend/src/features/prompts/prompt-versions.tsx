import { useQuery } from '@tanstack/react-query'
import { Clock3, Loader2, X } from 'lucide-react'
import { useState } from 'react'
import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'
import remarkGfm from 'remark-gfm'
import { listPromptVersions } from '@/api/prompts'
import { queryKeys } from '@/api/query-keys'
import type { PromptVersion } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

const agentLabels: Record<PromptVersion['targetAgent'], string> = {
  ClaudeCode: 'Claude Code',
  Codex: 'Codex',
}

const kindLabels: Record<PromptVersion['kind'], string> = {
  General: 'Geral',
  Planning: 'Planejamento',
}

const statusLabels: Record<PromptVersion['status'], string> = {
  Draft: 'Rascunho',
  Ready: 'Pronto',
  Archived: 'Arquivado',
}

export function PromptVersions({ promptId }: { promptId: string }) {
  const [selectedVersion, setSelectedVersion] = useState<PromptVersion | null>(null)

  const versionsQuery = useQuery({
    queryKey: queryKeys.prompts.versions(promptId),
    queryFn: () => listPromptVersions(promptId),
  })

  return (
    <aside className="grid content-start gap-3 rounded-lg border border-[#d9dfd5] bg-white p-4">
      <div className="flex items-center gap-2 text-sm font-semibold text-[#172126]">
        <Clock3 className="h-4 w-4 text-[#5e7461]" />
        Versoes
      </div>

      {versionsQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-[#66746b]">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando
        </div>
      ) : null}

      <div className="grid gap-2">
        {versionsQuery.data?.map((version) => (
          <button
            key={version.id}
            type="button"
            className={cn(
              'grid min-w-0 gap-1 rounded-md border p-3 text-left transition-colors',
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
        ))}
      </div>

      {selectedVersion ? (
        <PromptVersionPreview version={selectedVersion} onClose={() => setSelectedVersion(null)} />
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
              <Badge variant="blue">{agentLabels[version.targetAgent]}</Badge>
              <Badge variant="neutral">{kindLabels[version.kind]}</Badge>
              <Badge variant="green">{statusLabels[version.status]}</Badge>
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
