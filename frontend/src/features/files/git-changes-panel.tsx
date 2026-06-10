import { useIsFetching, useQueryClient } from '@tanstack/react-query'
import { FileText, Loader2, RefreshCw } from 'lucide-react'
import { useMemo } from 'react'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { GitFileStatus } from '@/api/schemas'
import { cn } from '@/lib/utils'
import { getGitStatusMeta } from './git-status-meta'
import { parentDirectoryPath } from './file-key'
import { useGitStatus } from './use-git-queries'

type GitChangesPanelProps = {
  workingDirectoryId: string
  selectedPath?: string | null
  onSelectChange: (entry: GitFileStatus) => void
  className?: string
}

export function GitChangesPanel({ workingDirectoryId, selectedPath, onSelectChange, className }: GitChangesPanelProps) {
  const queryClient = useQueryClient()
  const statusQuery = useGitStatus(workingDirectoryId)
  const fetchingCount = useIsFetching({ queryKey: queryKeys.git.status(workingDirectoryId) })
  const isRefreshing = fetchingCount > 0
  const entries = useMemo(
    () => [...(statusQuery.data ?? [])].sort((left, right) => left.path.localeCompare(right.path)),
    [statusQuery.data],
  )

  const handleRefresh = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.git.status(workingDirectoryId) })
  }

  return (
    <section
      className={cn(
        'grid min-h-0 grid-rows-[auto_minmax(0,1fr)] overflow-hidden rounded-lg border border-border bg-card',
        className,
      )}
    >
      <div className="flex items-center justify-between gap-2 border-b border-border px-3 py-2">
        <div className="min-w-0 text-xs font-semibold uppercase tracking-normal text-muted-foreground">
          Alteracoes (git)
          {entries.length ? <span className="ml-1 text-muted-foreground">({entries.length})</span> : null}
        </div>
        <button
          type="button"
          onClick={handleRefresh}
          disabled={isRefreshing}
          title="Recarregar alteracoes"
          aria-label="Recarregar alteracoes do git"
          className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:pointer-events-none disabled:opacity-60"
        >
          <RefreshCw className={cn('h-3.5 w-3.5', isRefreshing && 'animate-spin')} />
        </button>
      </div>

      <div className="min-h-0 overflow-auto p-1">
        {statusQuery.isLoading ? (
          <div className="flex items-center gap-2 px-2 py-2 text-xs text-muted-foreground">
            <Loader2 className="h-3.5 w-3.5 animate-spin" />
            Carregando
          </div>
        ) : null}

        {statusQuery.isError ? (
          <div className="px-2 py-2 text-xs text-destructive">{getErrorMessage(statusQuery.error)}</div>
        ) : null}

        {!statusQuery.isLoading && !statusQuery.isError && !entries.length ? (
          <div className="px-2 py-2 text-xs text-muted-foreground">Nenhuma alteracao detectada.</div>
        ) : null}

        <ul className="grid gap-0.5">
          {entries.map((entry) => {
            const meta = getGitStatusMeta(entry.status)
            const parentPath = parentDirectoryPath(entry.path)
            const fileName = entry.path.split('/').pop() || entry.path
            const isSelected = selectedPath === entry.path

            return (
              <li key={`${entry.status}:${entry.path}:${entry.originalPath ?? ''}`}>
                <button
                  type="button"
                  onClick={() => onSelectChange(entry)}
                  className={cn(
                    'flex w-full min-w-0 items-center gap-1.5 rounded-md px-1.5 py-1 text-left text-xs transition-colors hover:bg-muted',
                    isSelected && 'bg-accent text-foreground',
                  )}
                  title={entry.path}
                >
                  <FileText className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate font-mono">{fileName}</span>
                    {parentPath ? (
                      <span className="truncate text-[0.68rem] text-muted-foreground">{parentPath}</span>
                    ) : null}
                  </span>
                  <span
                    className={cn('shrink-0 rounded px-1 font-mono text-[0.65rem] font-semibold', meta.badgeClass)}
                    title={meta.label}
                  >
                    {meta.letter}
                  </span>
                </button>
              </li>
            )
          })}
        </ul>
      </div>
    </section>
  )
}
