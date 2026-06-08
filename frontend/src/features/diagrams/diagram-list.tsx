import { Archive, ArchiveRestore, Loader2, Plus, Search, Shapes, Trash2, Workflow } from 'lucide-react'
import type * as React from 'react'
import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import type { DiagramSummary, DiagramType, WorkingDirectory } from '@/api/schemas'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { useDiagramMutations, useDiagrams } from './use-diagrams'

type DiagramListProps = {
  /** Fixed workspace (workspace tab). Omit for the global cross-workspace list. */
  workspaceId?: string
  /** Workspaces available in global mode, used for the filter and the create target. */
  workspaces?: WorkingDirectory[]
  selectedDiagramId: string | null
  onSelect: (id: string | null) => void
}

// Seed content so a brand new diagram opens with something sensible.
const NEW_DIAGRAM_CONTENT: Record<DiagramType, string> = {
  Excalidraw: '',
  Mermaid: 'flowchart TD\n    A[Inicio] --> B[Fim]',
}

export function DiagramList({ workspaceId, workspaces, selectedDiagramId, onSelect }: DiagramListProps) {
  const isGlobal = !workspaceId
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState<DiagramType | ''>('')
  const [includeArchived, setIncludeArchived] = useState(false)
  const [showTypeMenu, setShowTypeMenu] = useState(false)
  const [workspaceFilter, setWorkspaceFilter] = useState('')
  const [createWorkspaceId, setCreateWorkspaceId] = useState('')

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), 300)
    return () => window.clearTimeout(handle)
  }, [search])

  const availableWorkspaces = workspaces ?? []
  const filterWorkspaceId = workspaceId ?? (workspaceFilter || undefined)

  const diagramsQuery = useDiagrams({
    workingDirectoryId: filterWorkspaceId,
    type: typeFilter || undefined,
    q: debouncedSearch || undefined,
    includeArchived,
  })
  const { create, archive, remove } = useDiagramMutations()

  const diagrams = diagramsQuery.data ?? []
  const canCreate = !isGlobal || availableWorkspaces.length > 0

  const toggleTypeMenu = () => {
    if (isGlobal && !showTypeMenu) {
      setCreateWorkspaceId((current) => current || workspaceFilter || availableWorkspaces[0]?.id || '')
    }
    setShowTypeMenu((open) => !open)
  }

  const handleCreate = (type: DiagramType) => {
    const target = workspaceId ?? createWorkspaceId
    if (!target) {
      toast.error('Selecione um workspace para criar o diagrama.')
      return
    }
    setShowTypeMenu(false)
    create.mutate(
      { workingDirectoryId: target, title: 'Novo diagrama', type, content: NEW_DIAGRAM_CONTENT[type] },
      {
        onSuccess: (diagram) => onSelect(diagram.id),
        onError: (error) => toast.error(getErrorMessage(error)),
      },
    )
  }

  const handleDelete = (diagram: DiagramSummary) => {
    if (!window.confirm(`Excluir o diagrama "${diagram.title}"? Esta acao nao pode ser desfeita.`)) {
      return
    }
    remove.mutate(diagram.id, {
      onSuccess: () => {
        if (selectedDiagramId === diagram.id) {
          onSelect(null)
        }
        toast.success('Diagrama excluido.')
      },
      onError: (error) => toast.error(getErrorMessage(error)),
    })
  }

  const emptyMessage = debouncedSearch
    ? 'Nenhum diagrama encontrado.'
    : isGlobal
      ? availableWorkspaces.length === 0
        ? 'Crie um diretorio de trabalho para comecar a desenhar diagramas.'
        : 'Nenhum diagrama ainda. Use "Novo" para criar.'
      : 'Nenhum diagrama neste workspace ainda.'

  return (
    <div className="flex min-h-0 flex-col gap-3 rounded-lg border border-border bg-card p-3">
      <div className="flex items-center gap-2">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar diagramas"
            className="pl-8"
            aria-label="Buscar diagramas"
          />
        </div>
        <Button
          type="button"
          size="sm"
          onClick={toggleTypeMenu}
          disabled={create.isPending || !canCreate}
          aria-expanded={showTypeMenu}
        >
          {create.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
          Novo
        </Button>
      </div>

      {showTypeMenu ? (
        <div className="flex flex-col gap-2 rounded-md border border-border bg-background p-2">
          {isGlobal ? (
            <Select
              value={createWorkspaceId}
              onChange={(event) => setCreateWorkspaceId(event.target.value)}
              aria-label="Workspace do novo diagrama"
              className="h-8 text-xs"
            >
              <option value="" disabled>
                Selecione um workspace
              </option>
              {availableWorkspaces.map((workspace) => (
                <option key={workspace.id} value={workspace.id}>
                  {workspace.name}
                </option>
              ))}
            </Select>
          ) : null}
          <div className="grid grid-cols-2 gap-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => handleCreate('Excalidraw')}
              disabled={isGlobal && !createWorkspaceId}
            >
              <Shapes className="h-4 w-4" />
              Excalidraw
            </Button>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => handleCreate('Mermaid')}
              disabled={isGlobal && !createWorkspaceId}
            >
              <Workflow className="h-4 w-4" />
              Mermaid
            </Button>
          </div>
        </div>
      ) : null}

      <div className="flex items-center gap-2">
        {isGlobal ? (
          <Select
            value={workspaceFilter}
            onChange={(event) => setWorkspaceFilter(event.target.value)}
            aria-label="Filtrar por workspace"
            className="h-8 flex-1 text-xs"
          >
            <option value="">Todos os workspaces</option>
            {availableWorkspaces.map((workspace) => (
              <option key={workspace.id} value={workspace.id}>
                {workspace.name}
              </option>
            ))}
          </Select>
        ) : null}
        <Select
          value={typeFilter}
          onChange={(event) => setTypeFilter(event.target.value as DiagramType | '')}
          aria-label="Filtrar por tipo"
          className="h-8 flex-1 text-xs"
        >
          <option value="">Todos os tipos</option>
          <option value="Excalidraw">Excalidraw</option>
          <option value="Mermaid">Mermaid</option>
        </Select>
      </div>

      <label className="flex items-center gap-2 px-0.5 text-xs text-muted-foreground">
        <input
          type="checkbox"
          checked={includeArchived}
          onChange={(event) => setIncludeArchived(event.target.checked)}
          className="h-3.5 w-3.5 rounded border-input"
        />
        Mostrar arquivados
      </label>

      <div className="min-h-0 flex-1 space-y-1.5 overflow-y-auto">
        {diagramsQuery.isLoading ? (
          <div className="flex items-center gap-2 p-3 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Carregando diagramas
          </div>
        ) : null}

        {diagramsQuery.isError ? (
          <div className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
            Nao foi possivel carregar os diagramas.
          </div>
        ) : null}

        {!diagramsQuery.isLoading && !diagramsQuery.isError && diagrams.length === 0 ? (
          <div className="rounded-md border border-dashed border-input p-4 text-center text-sm text-muted-foreground">
            {emptyMessage}
          </div>
        ) : null}

        {diagrams.map((diagram) => (
          <div
            key={diagram.id}
            role="button"
            tabIndex={0}
            onClick={() => onSelect(diagram.id)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault()
                onSelect(diagram.id)
              }
            }}
            className={cn(
              'group cursor-pointer rounded-md border border-transparent p-2.5 transition-colors hover:bg-accent',
              selectedDiagramId === diagram.id ? 'border-ring bg-accent' : null,
            )}
          >
            <div className="flex items-start justify-between gap-2">
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5">
                  {diagram.type === 'Excalidraw' ? (
                    <Shapes className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                  ) : (
                    <Workflow className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                  )}
                  <span className="truncate text-sm font-medium text-foreground">{diagram.title}</span>
                  {diagram.isArchived ? (
                    <span className="shrink-0 rounded bg-muted px-1.5 py-0.5 text-[0.62rem] text-muted-foreground">
                      Arquivado
                    </span>
                  ) : null}
                </div>
                <p className="mt-0.5 truncate text-xs text-muted-foreground">
                  {isGlobal ? (
                    <>
                      <span className="text-foreground/70">{diagram.workingDirectoryName}</span>
                      {diagram.description?.trim() ? ` · ${diagram.description}` : ''}
                    </>
                  ) : diagram.description?.trim() ? (
                    diagram.description
                  ) : (
                    diagram.type
                  )}
                </p>
              </div>
              <div className="flex shrink-0 items-center gap-0.5 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
                <IconAction
                  title={diagram.isArchived ? 'Desarquivar' : 'Arquivar'}
                  onClick={() =>
                    archive.mutate(
                      { id: diagram.id, isArchived: !diagram.isArchived },
                      { onError: (error) => toast.error(getErrorMessage(error)) },
                    )
                  }
                >
                  {diagram.isArchived ? <ArchiveRestore className="h-3.5 w-3.5" /> : <Archive className="h-3.5 w-3.5" />}
                </IconAction>
                <IconAction title="Excluir" onClick={() => handleDelete(diagram)} destructive>
                  <Trash2 className="h-3.5 w-3.5" />
                </IconAction>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

function IconAction({
  title,
  onClick,
  destructive,
  children,
}: {
  title: string
  onClick: () => void
  destructive?: boolean
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      title={title}
      aria-label={title}
      onClick={(event) => {
        event.stopPropagation()
        onClick()
      }}
      className={cn(
        'rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground',
        destructive ? 'hover:text-destructive' : null,
      )}
    >
      {children}
    </button>
  )
}
