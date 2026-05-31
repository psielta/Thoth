import { Link } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { FileText, Loader2, Plus, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { listPrompts } from '@/api/prompts'
import { queryKeys } from '@/api/query-keys'
import type { PromptKind, PromptStatus, TargetAgent } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import {
  AGENT_LABELS,
  AGENT_OPTIONS,
  KIND_LABELS,
  KIND_OPTIONS,
  STATUS_BADGE_VARIANTS,
  STATUS_LABELS,
  STATUS_OPTIONS,
} from './constants'

type PromptListProps = {
  workingDirectoryId: string
}

export function PromptList({ workingDirectoryId }: PromptListProps) {
  const [q, setQ] = useState('')
  const [status, setStatus] = useState<PromptStatus | ''>('')
  const [agent, setAgent] = useState<TargetAgent | ''>('')
  const [kind, setKind] = useState<PromptKind | ''>('')

  const filters = useMemo(
    () => ({
      workingDirectoryId,
      rootOnly: true,
      q: q.trim() || undefined,
      status: status || undefined,
      agent: agent || undefined,
      kind: kind || undefined,
    }),
    [agent, kind, q, status, workingDirectoryId],
  )

  const promptsQuery = useQuery({
    queryKey: queryKeys.prompts.list(filters),
    queryFn: () => listPrompts(filters),
  })

  return (
    <section className="grid gap-4">
      <div className="flex flex-col gap-3 rounded-lg border border-[#d9dfd5] bg-white p-4 lg:flex-row lg:items-end">
        <label className="grid flex-1 gap-1.5 text-sm font-medium text-[#253035]">
          Buscar
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[#66746b]" />
            <Input className="pl-9" value={q} onChange={(event) => setQ(event.target.value)} />
          </div>
        </label>

        <label className="grid gap-1.5 text-sm font-medium text-[#253035] lg:w-40">
          Status
          <Select value={status} onChange={(event) => setStatus(event.target.value as PromptStatus | '')}>
            <option value="">Todos</option>
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </label>

        <label className="grid gap-1.5 text-sm font-medium text-[#253035] lg:w-40">
          Agente
          <Select value={agent} onChange={(event) => setAgent(event.target.value as TargetAgent | '')}>
            <option value="">Todos</option>
            {AGENT_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </label>

        <label className="grid gap-1.5 text-sm font-medium text-[#253035] lg:w-40">
          Tipo
          <Select value={kind} onChange={(event) => setKind(event.target.value as PromptKind | '')}>
            <option value="">Todos</option>
            {KIND_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </label>

        <Link to="/workspaces/$workspaceId/prompts/new" params={{ workspaceId: workingDirectoryId }}>
          <Button type="button" className="w-full lg:w-auto">
            <Plus className="h-4 w-4" />
            Novo prompt
          </Button>
        </Link>
      </div>

      {promptsQuery.isLoading ? (
        <div className="flex items-center gap-2 rounded-lg border border-[#d9dfd5] bg-white p-4 text-sm text-[#66746b]">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando prompts
        </div>
      ) : null}

      {!promptsQuery.isLoading && !promptsQuery.data?.length ? (
        <div className="rounded-lg border border-dashed border-[#cbd5c8] bg-white p-6 text-sm text-[#66746b]">
          Nenhum prompt encontrado.
        </div>
      ) : null}

      <div className="grid gap-2">
        {promptsQuery.data?.map((prompt) => (
          <Link
            key={prompt.id}
            to="/workspaces/$workspaceId/prompts/$promptId"
            params={{ workspaceId: workingDirectoryId, promptId: prompt.id }}
            className="rounded-lg border border-[#d9dfd5] bg-white p-4 transition-colors hover:border-[#8aa083] hover:bg-[#fbfcfa]"
          >
            <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
              <div className="min-w-0">
                <div className="flex items-center gap-2 text-sm font-semibold text-[#172126]">
                  <FileText className="h-4 w-4 shrink-0 text-[#5e7461]" />
                  <span className="truncate">{prompt.title}</span>
                </div>
                <p className="mt-1 line-clamp-2 text-sm text-[#66746b]">{prompt.content}</p>
              </div>
              <div className="flex shrink-0 flex-wrap gap-2">
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
