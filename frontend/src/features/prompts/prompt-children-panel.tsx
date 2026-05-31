import { Link } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { Loader2, MessageSquareText } from 'lucide-react'
import { useMemo } from 'react'
import { listPrompts } from '@/api/prompts'
import { queryKeys } from '@/api/query-keys'
import type { PromptStatus } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import {
  AGENT_LABELS,
  KIND_LABELS,
  STATUS_BADGE_VARIANTS,
  STATUS_LABELS,
} from './constants'

type PromptChildrenPanelProps = {
  workingDirectoryId: string
  parentPromptId: string
}

export function PromptChildrenPanel({ workingDirectoryId, parentPromptId }: PromptChildrenPanelProps) {
  const filters = useMemo(
    () => ({ workingDirectoryId, parentPromptId }),
    [parentPromptId, workingDirectoryId],
  )
  const childrenQuery = useQuery({
    queryKey: queryKeys.prompts.list(filters),
    queryFn: () => listPrompts(filters),
  })

  return (
    <section className="grid gap-3 rounded-lg border border-[#d9dfd5] bg-white p-4">
      <div className="flex items-center gap-2 text-sm font-semibold text-[#172126]">
        <MessageSquareText className="h-4 w-4 text-[#5e7461]" />
        Prompts filhos
      </div>

      {childrenQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-[#66746b]">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando
        </div>
      ) : null}

      {!childrenQuery.isLoading && !childrenQuery.data?.length ? (
        <div className="rounded-md border border-dashed border-[#cbd5c8] bg-[#fbfcfa] p-3 text-sm text-[#66746b]">
          Nenhum prompt filho.
        </div>
      ) : null}

      <div className="grid gap-2">
        {childrenQuery.data?.map((prompt) => (
          <Link
            key={prompt.id}
            to="/workspaces/$workspaceId/prompts/$promptId"
            params={{ workspaceId: workingDirectoryId, promptId: prompt.id }}
            className="grid min-w-0 gap-2 rounded-md border border-[#d9dfd5] p-3 text-left transition-colors hover:border-[#8aa083] hover:bg-[#fbfcfa]"
          >
            <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
              <div className="min-w-0">
                <div className="truncate text-sm font-medium text-[#172126]">{prompt.title}</div>
                <p className="mt-1 line-clamp-2 text-sm text-[#66746b]">{prompt.content}</p>
              </div>
              <div className="flex shrink-0 flex-wrap gap-1.5">
                <StatusBadge status={prompt.status} />
                <Badge variant="blue">{AGENT_LABELS[prompt.targetAgent]}</Badge>
                <Badge>{KIND_LABELS[prompt.kind]}</Badge>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </section>
  )
}

function StatusBadge({ status }: { status: PromptStatus }) {
  return <Badge variant={STATUS_BADGE_VARIANTS[status]}>{STATUS_LABELS[status]}</Badge>
}
