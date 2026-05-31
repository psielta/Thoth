import { useQuery } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import { Loader2, Search, Settings2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { listWorkingDirectories } from '@/api/working-directories'
import { getBoard, getWorkflowTemplate } from '@/api/workflow'
import { queryKeys } from '@/api/query-keys'
import type { PromptStatus, PromptWorkflowStatus } from '@/api/schemas'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { usePromptHub } from '@/realtime/prompt-hub'
import { buildColumns } from './board-columns'
import { TaskCard } from './task-card'

const PROMPT_STATUS_OPTIONS: Array<{ value: PromptStatus | ''; label: string }> = [
  { value: '', label: 'Não arquivadas' },
  { value: 'Draft', label: 'Rascunho' },
  { value: 'Ready', label: 'Pronto' },
  { value: 'Archived', label: 'Arquivadas' },
]

export function Board() {
  const hub = usePromptHub()
  const { joinTasks, leaveTasks } = hub
  const [q, setQ] = useState('')
  const [workingDirectoryId, setWorkingDirectoryId] = useState('')
  const [workflowStatus, setWorkflowStatus] = useState<PromptWorkflowStatus | ''>('')
  const [promptStatus, setPromptStatus] = useState<PromptStatus | ''>('')

  useEffect(() => {
    joinTasks()
    return () => leaveTasks()
  }, [joinTasks, leaveTasks])

  const filters = useMemo(
    () => ({
      q: q.trim() || undefined,
      workingDirectoryId: workingDirectoryId || undefined,
      workflowStatus: workflowStatus || undefined,
      promptStatus: promptStatus || undefined,
    }),
    [promptStatus, q, workflowStatus, workingDirectoryId],
  )

  const boardQuery = useQuery({
    queryKey: queryKeys.workflow.board(filters),
    queryFn: () => getBoard(filters),
  })
  const templateQuery = useQuery({
    queryKey: queryKeys.workflow.template(),
    queryFn: getWorkflowTemplate,
  })
  const workspacesQuery = useQuery({
    queryKey: queryKeys.workingDirectories.all,
    queryFn: listWorkingDirectories,
  })

  const columns = useMemo(
    () => buildColumns(boardQuery.data ?? [], templateQuery.data?.phases.map((phase) => phase.name) ?? []),
    [boardQuery.data, templateQuery.data],
  )
  const total = boardQuery.data?.length ?? 0

  return (
    <section className="grid gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-[#172126]">Quadro de tarefas</h1>
          <p className="mt-1 text-sm text-[#66746b]">Acompanhe a fase e o responsável de cada tarefa, em todos os diretórios.</p>
        </div>
        <Link to="/settings">
          <Button type="button" variant="secondary" size="sm">
            <Settings2 className="h-4 w-4" />
            Configurar fases
          </Button>
        </Link>
      </div>

      <div className="flex flex-col gap-3 rounded-lg border border-[#d9dfd5] bg-white p-4 lg:flex-row lg:items-end">
        <label className="grid flex-1 gap-1.5 text-sm font-medium text-[#253035]">
          Buscar
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[#66746b]" />
            <Input className="pl-9" value={q} onChange={(event) => setQ(event.target.value)} placeholder="Título ou conteúdo" />
          </div>
        </label>
        <label className="grid gap-1.5 text-sm font-medium text-[#253035] lg:w-48">
          Diretório
          <Select value={workingDirectoryId} onChange={(event) => setWorkingDirectoryId(event.target.value)}>
            <option value="">Todos</option>
            {workspacesQuery.data?.map((workspace) => (
              <option key={workspace.id} value={workspace.id}>
                {workspace.name}
              </option>
            ))}
          </Select>
        </label>
        <label className="grid gap-1.5 text-sm font-medium text-[#253035] lg:w-40">
          Fluxo
          <Select value={workflowStatus} onChange={(event) => setWorkflowStatus(event.target.value as PromptWorkflowStatus | '')}>
            <option value="">Todos</option>
            <option value="Active">Em andamento</option>
            <option value="Done">Concluídas</option>
          </Select>
        </label>
        <label className="grid gap-1.5 text-sm font-medium text-[#253035] lg:w-40">
          Prompts
          <Select value={promptStatus} onChange={(event) => setPromptStatus(event.target.value as PromptStatus | '')}>
            {PROMPT_STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </label>
      </div>

      {boardQuery.isLoading ? (
        <div className="flex items-center gap-2 rounded-lg border border-[#d9dfd5] bg-white p-4 text-sm text-[#66746b]">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando tarefas
        </div>
      ) : total === 0 ? (
        <div className="rounded-lg border border-dashed border-[#cbd5c8] bg-white p-6 text-sm text-[#66746b]">
          Nenhuma tarefa encontrada. Crie um prompt em um diretório para começar.
        </div>
      ) : (
        <div className="flex gap-4 overflow-x-auto pb-2">
          {columns.map((column) => (
            <div key={column.title} className="flex w-72 shrink-0 flex-col gap-3">
              <div className="flex items-center justify-between rounded-md bg-[#eef2eb] px-3 py-2">
                <span className="text-sm font-semibold text-[#2c3a31]">{column.title}</span>
                <span className="rounded-full bg-white px-2 py-0.5 text-xs text-[#66746b]">{column.tasks.length}</span>
              </div>
              <div className="grid gap-2">
                {column.tasks.map((task) => (
                  <TaskCard key={task.promptId} task={task} />
                ))}
                {column.tasks.length === 0 ? (
                  <p className="rounded-md border border-dashed border-[#d9dfd5] px-3 py-4 text-center text-xs text-[#8a958c]">Vazio</p>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
