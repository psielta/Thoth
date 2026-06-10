import { useIsFetching, useQueryClient } from '@tanstack/react-query'
import { ChevronRight, FileText, Folder, Loader2, RefreshCw, Search } from 'lucide-react'
import type { CSSProperties, DragEvent } from 'react'
import { useMemo, useState } from 'react'
import { queryKeys } from '@/api/query-keys'
import type { FileSearchResult, FileTreeNode, GitFileStatusValue } from '@/api/schemas'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { cn } from '@/lib/utils'
import { createFileKey, parentDirectoryPath } from './file-key'
import { getGitStatusMeta } from './git-status-meta'
import { useDirectoryChildren, useFileSearch } from './use-file-queries'
import { useGitStatus } from './use-git-queries'

export const WORKSPACE_FILE_MIME = 'application/x-workspace-file'

type WorkspaceFileTreeProps = {
  workingDirectoryId: string
  selectedPath?: string | null
  onSelectFile?: (relativePath: string) => void
  onOpenFile?: (relativePath: string) => void
  className?: string
  style?: CSSProperties
}

export function WorkspaceFileTree({
  workingDirectoryId,
  selectedPath,
  onSelectFile,
  onOpenFile,
  className,
  style,
}: WorkspaceFileTreeProps) {
  const queryClient = useQueryClient()
  const rootQuery = useDirectoryChildren(workingDirectoryId, '')
  const [expandedPaths, setExpandedPaths] = useState<Set<string>>(() => new Set())
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search.trim(), 250)
  const isSearching = debouncedSearch.length >= 2
  const searchQuery = useFileSearch(workingDirectoryId, debouncedSearch, isSearching)
  const gitStatusQuery = useGitStatus(workingDirectoryId)
  const fileRefreshingCount = useIsFetching({ queryKey: queryKeys.files.trees(workingDirectoryId) })
  const gitRefreshingCount = useIsFetching({ queryKey: queryKeys.git.status(workingDirectoryId) })
  const refreshingCount = fileRefreshingCount + gitRefreshingCount
  const isRefreshing = refreshingCount > 0

  const { statusByKey, changedDirKeys } = useMemo(() => {
    const nextStatusByKey = new Map<string, GitFileStatusValue>()
    const nextChangedDirKeys = new Set<string>()

    for (const entry of gitStatusQuery.data ?? []) {
      nextStatusByKey.set(createFileKey(entry.path), entry.status)
      let currentParent = parentDirectoryPath(entry.path)
      while (currentParent) {
        nextChangedDirKeys.add(createFileKey(currentParent))
        currentParent = parentDirectoryPath(currentParent)
      }
    }

    return { statusByKey: nextStatusByKey, changedDirKeys: nextChangedDirKeys }
  }, [gitStatusQuery.data])

  const handleRefresh = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.files.trees(workingDirectoryId) })
    void queryClient.invalidateQueries({ queryKey: queryKeys.files.searches(workingDirectoryId) })
    void queryClient.invalidateQueries({ queryKey: queryKeys.git.status(workingDirectoryId) })
  }

  const toggleExpanded = (relativePath: string) => {
    setExpandedPaths((current) => {
      const next = new Set(current)
      if (next.has(relativePath)) {
        next.delete(relativePath)
      } else {
        next.add(relativePath)
      }
      return next
    })
  }

  const nodes = rootQuery.data ?? []

  return (
    <section
      className={cn(
        'grid min-h-0 grid-rows-[auto_minmax(0,1fr)] overflow-hidden rounded-lg border border-border bg-card',
        className,
      )}
      style={style}
    >
      <div className="grid gap-2 border-b border-border px-3 py-2">
        <div className="flex items-center justify-between gap-2">
          <div className="text-xs font-semibold uppercase tracking-normal text-muted-foreground">
            Arquivos do workspace
          </div>
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isRefreshing}
            title="Recarregar arquivos"
            aria-label="Recarregar arquivos do workspace"
            className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:pointer-events-none disabled:opacity-60"
          >
            <RefreshCw className={cn('h-3.5 w-3.5', isRefreshing && 'animate-spin')} />
          </button>
        </div>
        <div className="relative">
          <Search className="pointer-events-none absolute left-2 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar arquivos por nome"
            className="h-8 w-full rounded-md border border-input bg-card pl-7 pr-2 text-xs text-foreground outline-none transition-colors focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>
      </div>

      <div className="min-h-0 overflow-auto p-1">
        {isSearching ? (
          <>
            {searchQuery.isLoading ? (
              <div className="flex items-center gap-2 px-2 py-2 text-xs text-muted-foreground">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Buscando
              </div>
            ) : null}

            {searchQuery.isError ? (
              <div className="px-2 py-2 text-xs text-destructive">Nao foi possivel buscar os arquivos.</div>
            ) : null}

            {!searchQuery.isLoading && !searchQuery.data?.length ? (
              <div className="px-2 py-2 text-xs text-muted-foreground">Nenhum arquivo encontrado.</div>
            ) : null}

            <ul className="grid gap-0.5">
              {searchQuery.data?.map((result) => (
                <SearchResultItem
                  key={result.relativePath}
                  result={result}
                  selectedPath={selectedPath}
                  onSelectFile={onSelectFile}
                  onOpenFile={onOpenFile}
                />
              ))}
            </ul>
          </>
        ) : (
          <>
            {rootQuery.isLoading ? (
              <div className="flex items-center gap-2 px-2 py-2 text-xs text-muted-foreground">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Carregando arvore
              </div>
            ) : null}

            {rootQuery.isError ? (
              <div className="px-2 py-2 text-xs text-destructive">Nao foi possivel carregar os arquivos.</div>
            ) : null}

            {!rootQuery.isLoading && !nodes.length ? (
              <div className="px-2 py-2 text-xs text-muted-foreground">Diretorio vazio.</div>
            ) : null}

            <ul className="grid gap-0.5">
              {nodes.map((node) => (
                <TreeNode
                  key={node.relativePath}
                  workingDirectoryId={workingDirectoryId}
                  node={node}
                  depth={0}
                  expandedPaths={expandedPaths}
                  selectedPath={selectedPath}
                  statusByKey={statusByKey}
                  changedDirKeys={changedDirKeys}
                  onToggleExpanded={toggleExpanded}
                  onSelectFile={onSelectFile}
                  onOpenFile={onOpenFile}
                />
              ))}
            </ul>
          </>
        )}
      </div>
    </section>
  )
}

type SearchResultItemProps = {
  result: FileSearchResult
  selectedPath?: string | null
  onSelectFile?: (relativePath: string) => void
  onOpenFile?: (relativePath: string) => void
}

function SearchResultItem({ result, selectedPath, onSelectFile, onOpenFile }: SearchResultItemProps) {
  const isSelected = !result.isDirectory && selectedPath === result.relativePath

  const handleClick = () => {
    if (result.isDirectory) {
      return
    }

    onSelectFile?.(result.relativePath)
    onOpenFile?.(result.relativePath)
  }

  return (
    <li>
      <button
        type="button"
        onClick={handleClick}
        disabled={result.isDirectory}
        className={cn(
          'flex w-full min-w-0 items-center gap-1.5 rounded-md px-1.5 py-1 text-left text-xs transition-colors',
          result.isDirectory ? 'cursor-default' : 'hover:bg-muted',
          isSelected && 'bg-accent text-foreground',
        )}
        title={result.relativePath}
      >
        {result.isDirectory ? (
          <Folder className="h-3.5 w-3.5 shrink-0 text-warning-solid" />
        ) : (
          <FileText className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        )}
        <span className="flex min-w-0 flex-col">
          <span className="truncate font-mono">{result.fileName}</span>
          <span className="truncate text-[0.68rem] text-muted-foreground">{result.relativePath}</span>
        </span>
      </button>
    </li>
  )
}

type TreeNodeProps = {
  workingDirectoryId: string
  node: FileTreeNode
  depth: number
  expandedPaths: Set<string>
  selectedPath?: string | null
  statusByKey: Map<string, GitFileStatusValue>
  changedDirKeys: Set<string>
  onToggleExpanded: (relativePath: string) => void
  onSelectFile?: (relativePath: string) => void
  onOpenFile?: (relativePath: string) => void
}

function TreeNode({
  workingDirectoryId,
  node,
  depth,
  expandedPaths,
  selectedPath,
  statusByKey,
  changedDirKeys,
  onToggleExpanded,
  onSelectFile,
  onOpenFile,
}: TreeNodeProps) {
  const isExpanded = expandedPaths.has(node.relativePath)
  const childrenQuery = useDirectoryChildren(workingDirectoryId, node.relativePath, node.isDirectory && isExpanded)
  const children = useMemo(() => childrenQuery.data ?? [], [childrenQuery.data])
  const isSelected = !node.isDirectory && selectedPath === node.relativePath
  const status = node.isDirectory ? undefined : statusByKey.get(createFileKey(node.relativePath))
  const meta = status ? getGitStatusMeta(status) : null
  const hasDirectoryChanges = node.isDirectory && changedDirKeys.has(createFileKey(node.relativePath))

  const handleClick = () => {
    if (node.isDirectory) {
      onToggleExpanded(node.relativePath)
      return
    }

    onSelectFile?.(node.relativePath)
    onOpenFile?.(node.relativePath)
  }

  const handleDragStart = (event: DragEvent<HTMLButtonElement>) => {
    if (node.isDirectory) {
      event.preventDefault()
      return
    }

    event.dataTransfer.effectAllowed = 'copy'
    event.dataTransfer.setData(
      WORKSPACE_FILE_MIME,
      JSON.stringify({
        workingDirectoryId,
        relativePath: node.relativePath,
      }),
    )
    event.dataTransfer.setData('text/plain', `@${node.relativePath}`)
  }

  return (
    <li>
      <button
        type="button"
        draggable={!node.isDirectory}
        onClick={handleClick}
        onDragStart={handleDragStart}
        className={cn(
          'flex w-full min-w-0 items-center gap-1 rounded-md px-1.5 py-1 text-left text-xs transition-colors hover:bg-muted',
          isSelected && 'bg-accent text-foreground',
        )}
        style={{ paddingLeft: `${depth * 0.85 + 0.35}rem` }}
        title={node.relativePath}
      >
        {node.isDirectory ? (
          <ChevronRight
            className={cn('h-3.5 w-3.5 shrink-0 text-muted-foreground transition-transform', isExpanded && 'rotate-90')}
          />
        ) : (
          <span className="inline-block h-3.5 w-3.5 shrink-0" />
        )}
        {node.isDirectory ? (
          <Folder className="h-3.5 w-3.5 shrink-0 text-warning-solid" />
        ) : (
          <FileText className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        )}
        <span className="truncate font-mono">{node.name}</span>
        {meta ? (
          <span
            className={cn('ml-auto shrink-0 rounded px-1 font-mono text-[0.65rem] font-semibold', meta.badgeClass)}
            title={meta.label}
          >
            {meta.letter}
          </span>
        ) : null}
        {hasDirectoryChanges ? (
          <span className="ml-auto h-1.5 w-1.5 shrink-0 rounded-full bg-warning-solid/70" title="Alteracoes no diretorio" />
        ) : null}
      </button>

      {node.isDirectory && isExpanded ? (
        <div className="grid gap-0.5">
          {childrenQuery.isLoading ? (
            <div
              className="flex items-center gap-1.5 py-1 text-[0.68rem] text-muted-foreground"
              style={{ paddingLeft: `${(depth + 1) * 0.85 + 0.35}rem` }}
            >
              <Loader2 className="h-3 w-3 animate-spin" />
              Carregando
            </div>
          ) : null}

          <ul className="grid gap-0.5">
            {children.map((child) => (
              <TreeNode
                key={child.relativePath}
                workingDirectoryId={workingDirectoryId}
                node={child}
                depth={depth + 1}
                expandedPaths={expandedPaths}
                selectedPath={selectedPath}
                statusByKey={statusByKey}
                changedDirKeys={changedDirKeys}
                onToggleExpanded={onToggleExpanded}
                onSelectFile={onSelectFile}
                onOpenFile={onOpenFile}
              />
            ))}
          </ul>
        </div>
      ) : null}
    </li>
  )
}
