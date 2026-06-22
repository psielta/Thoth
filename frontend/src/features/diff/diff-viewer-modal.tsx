import { AlertTriangle, ChevronDown, ChevronUp, X } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { getErrorMessage } from '@/api/client'
import { Button } from '@/components/ui/button'
import { useMediaQuery } from '@/hooks/use-media-query'
import { computeLineDiff } from './diff-engine'
import { DiffViewer } from './diff-viewer'
import { useDiffNavigation } from './use-diff-navigation'

type DiffViewerModalProps = {
  oldContent: string
  newContent: string
  oldLabel: string
  newLabel: string
  isLoading?: boolean
  error?: unknown
  onClose: () => void
}

export function DiffViewerModal({
  oldContent,
  newContent,
  oldLabel,
  newLabel,
  isLoading = false,
  error,
  onClose,
}: DiffViewerModalProps) {
  const [viewMode, setViewMode] = useState<'split' | 'unified'>('split')
  const isDesktop = useMediaQuery('(min-width: 768px)')
  const effectiveMode = isDesktop ? viewMode : 'unified'

  const model = useMemo(
    () => (isLoading || error ? null : computeLineDiff(oldContent, newContent)),
    [oldContent, newContent, isLoading, error],
  )

  const hunkStarts = model
    ? effectiveMode === 'unified'
      ? model.changeHunks.unified
      : model.changeHunks.split
    : []

  const navigationEnabled = Boolean(model?.hasChanges) && !isLoading && !error

  const { activeIndex, totalHunks, canGoNext, canGoPrevious, goToNext, goToPrevious, registerHunkRef } =
    useDiffNavigation(hunkStarts, navigationEnabled)

  const requestClose = useCallback(() => onClose(), [onClose])

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        requestClose()
        return
      }
      if (!navigationEnabled) return
      if (e.altKey && e.key === 'ArrowDown') {
        e.preventDefault()
        goToNext()
      }
      if (e.altKey && e.key === 'ArrowUp') {
        e.preventDefault()
        goToPrevious()
      }
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [requestClose, navigationEnabled, goToNext, goToPrevious])

  return (
    <div
      className="fixed inset-0 z-50 grid bg-black/50 p-4 backdrop-blur-sm sm:p-6"
      role="dialog"
      aria-modal="true"
      aria-labelledby="diff-modal-title"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) requestClose()
      }}
    >
      <div className="mx-auto grid h-full w-full max-w-7xl grid-rows-[auto_minmax(0,1fr)] overflow-hidden rounded-lg border border-border bg-card shadow-2xl">
        <div className="flex min-w-0 items-center justify-between gap-3 border-b border-border p-4">
          <div className="flex min-w-0 items-center gap-3">
            <h2 id="diff-modal-title" className="shrink-0 text-base font-semibold text-foreground">
              Comparar versoes
            </h2>
            <span className="truncate text-sm text-muted-foreground">
              {oldLabel} → {newLabel}
            </span>
          </div>

          <div className="flex shrink-0 items-center gap-2">
            {navigationEnabled && totalHunks > 0 ? (
              <div className="flex items-center gap-1">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={!canGoPrevious}
                  onClick={goToPrevious}
                  aria-label="Diferenca anterior"
                >
                  <ChevronUp className="h-4 w-4" />
                  <span className="hidden sm:inline">Anterior</span>
                </Button>
                <span className="min-w-[3.5rem] text-center text-sm text-muted-foreground" aria-live="polite">
                  {activeIndex + 1} / {totalHunks}
                </span>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={!canGoNext}
                  onClick={goToNext}
                  aria-label="Proxima diferenca"
                >
                  <span className="hidden sm:inline">Proxima</span>
                  <ChevronDown className="h-4 w-4" />
                </Button>
              </div>
            ) : null}

            <div className="hidden items-center gap-1 md:flex">
              <Button
                type="button"
                variant={effectiveMode === 'split' ? 'default' : 'ghost'}
                size="sm"
                onClick={() => setViewMode('split')}
              >
                Lado a lado
              </Button>
              <Button
                type="button"
                variant={effectiveMode === 'unified' ? 'default' : 'ghost'}
                size="sm"
                onClick={() => setViewMode('unified')}
              >
                Unificado
              </Button>
            </div>

            <Button type="button" variant="ghost" size="icon" onClick={requestClose} aria-label="Fechar">
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <div className="min-h-0 overflow-auto">
          {isLoading ? (
            <LoadingSkeleton />
          ) : error ? (
            <ErrorState error={error} />
          ) : (
            <DiffViewer
              oldContent={oldContent}
              newContent={newContent}
              oldLabel={oldLabel}
              newLabel={newLabel}
              viewMode={effectiveMode}
              model={model ?? undefined}
              activeHunkIndex={navigationEnabled ? activeIndex : null}
              registerHunkRef={registerHunkRef}
            />
          )}
        </div>
      </div>
    </div>
  )
}

function LoadingSkeleton() {
  return (
    <div className="grid gap-2 p-4">
      {[70, 90, 55, 80, 65, 75, 50, 85].map((w, i) => (
        <div
          key={i}
          className="h-4 animate-pulse rounded bg-muted"
          style={{ width: `${w}%` }}
        />
      ))}
    </div>
  )
}

function ErrorState({ error }: { error: unknown }) {
  return (
    <div className="m-4 flex items-start gap-2 rounded-md border border-danger-border bg-danger-soft p-3 text-sm text-danger-soft-foreground">
      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
      <span>{getErrorMessage(error)}</span>
    </div>
  )
}