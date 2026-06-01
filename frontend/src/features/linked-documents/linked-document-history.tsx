import { Clock3, GitCompare, Loader2 } from 'lucide-react'
import type { LinkedDocumentVersion } from '@/api/schemas'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type LinkedDocumentHistoryProps = {
  currentVersion: number
  selectedVersion?: number
  versions?: LinkedDocumentVersion[]
  isLoading: boolean
  onSelectVersion: (version?: number) => void
  compareSelection: number[]
  onToggleCompare: (version: number) => void
  onClearCompare: () => void
  onCompare: () => void
}

const sourceLabels: Record<LinkedDocumentVersion['source'], string> = {
  Initial: 'Inicial',
  FileChanged: 'Arquivo alterado',
  ManualRefresh: 'Atualizacao manual',
  Resumed: 'Retomado',
}

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

export function LinkedDocumentHistory({
  currentVersion,
  selectedVersion,
  versions,
  isLoading,
  onSelectVersion,
  compareSelection,
  onToggleCompare,
  onClearCompare,
  onCompare,
}: LinkedDocumentHistoryProps) {
  return (
    <aside className="grid content-start gap-3 rounded-lg border border-border bg-card p-3">
      <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
        <Clock3 className="h-4 w-4 text-ring" />
        Versoes
      </div>

      <Button
        type="button"
        variant={selectedVersion ? 'secondary' : 'default'}
        size="sm"
        onClick={() => onSelectVersion(undefined)}
      >
        Atual v{currentVersion || 1}
      </Button>

      {compareSelection.length > 0 && (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            disabled={compareSelection.length !== 2}
            onClick={onCompare}
          >
            <GitCompare className="h-4 w-4" />
            Comparar versoes
          </Button>
          <button
            type="button"
            className="text-xs text-muted-foreground underline"
            onClick={onClearCompare}
          >
            Limpar
          </button>
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando
        </div>
      ) : null}

      <div className="grid gap-2">
        {versions?.map((version) => {
          const isChecked = compareSelection.includes(version.versionNumber)
          const isDisabled = compareSelection.length >= 2 && !isChecked
          return (
            <div key={version.id} className="flex items-start gap-2">
              <input
                type="checkbox"
                id={`compare-plan-${version.id}`}
                className="mt-2 h-4 w-4 shrink-0 accent-primary"
                checked={isChecked}
                onChange={() => onToggleCompare(version.versionNumber)}
                disabled={isDisabled}
                aria-label={`Selecionar v${version.versionNumber} para comparacao`}
              />
              <button
                type="button"
                className={cn(
                  'grid min-w-0 flex-1 gap-1 rounded-md border p-2 text-left text-xs transition-colors',
                  selectedVersion === version.versionNumber
                    ? 'border-primary bg-muted text-foreground'
                    : 'border-border bg-card text-muted-foreground hover:bg-background',
                )}
                onClick={() => onSelectVersion(version.versionNumber)}
              >
                <span className="font-semibold text-foreground">v{version.versionNumber}</span>
                <span className="truncate">{sourceLabels[version.source]}</span>
                <span className="truncate text-muted-foreground">{dateFormatter.format(new Date(version.createdAtUtc))}</span>
                <span className="truncate text-muted-foreground">{formatBytes(version.sizeBytes)}</span>
              </button>
            </div>
          )
        })}
      </div>
    </aside>
  )
}

function formatBytes(sizeBytes: number) {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`
  }

  if (sizeBytes < 1024 * 1024) {
    return `${(sizeBytes / 1024).toFixed(1)} KB`
  }

  return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`
}
