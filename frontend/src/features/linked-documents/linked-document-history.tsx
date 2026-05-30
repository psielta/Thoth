import { Clock3, Loader2 } from 'lucide-react'
import type { LinkedDocumentVersion } from '@/api/schemas'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type LinkedDocumentHistoryProps = {
  currentVersion: number
  selectedVersion?: number
  versions?: LinkedDocumentVersion[]
  isLoading: boolean
  onSelectVersion: (version?: number) => void
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
}: LinkedDocumentHistoryProps) {
  return (
    <aside className="grid content-start gap-3 rounded-lg border border-[#d9dfd5] bg-white p-3">
      <div className="flex items-center gap-2 text-sm font-semibold text-[#172126]">
        <Clock3 className="h-4 w-4 text-[#5e7461]" />
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

      {isLoading ? (
        <div className="flex items-center gap-2 text-sm text-[#66746b]">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando
        </div>
      ) : null}

      <div className="grid gap-2">
        {versions?.map((version) => (
          <button
            key={version.id}
            type="button"
            className={cn(
              'grid min-w-0 gap-1 rounded-md border p-2 text-left text-xs transition-colors',
              selectedVersion === version.versionNumber
                ? 'border-[#254632] bg-[#eef2eb] text-[#172126]'
                : 'border-[#d9dfd5] bg-white text-[#425048] hover:bg-[#f7f8f6]',
            )}
            onClick={() => onSelectVersion(version.versionNumber)}
          >
            <span className="font-semibold text-[#172126]">v{version.versionNumber}</span>
            <span className="truncate">{sourceLabels[version.source]}</span>
            <span className="truncate text-[#66746b]">{dateFormatter.format(new Date(version.createdAtUtc))}</span>
            <span className="truncate text-[#66746b]">{formatBytes(version.sizeBytes)}</span>
          </button>
        ))}
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
