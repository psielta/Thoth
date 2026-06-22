import { useMemo, type ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { computeLineDiff } from './diff-engine'
import type { DiffModel, DiffRowType, DiffSegment, SplitCell, SplitRow, UnifiedRow } from './diff-engine'

type ViewMode = 'split' | 'unified'

type DiffViewerProps = {
  oldContent: string
  newContent: string
  oldLabel: string
  newLabel: string
  viewMode: ViewMode
  model?: DiffModel
  activeHunkIndex?: number | null
  registerHunkRef?: (hunkIndex: number, el: HTMLElement | null) => void
}

const rowBg: Record<DiffRowType, string> = {
  removed: 'bg-red-500/20',
  added: 'bg-green-500/20',
  unchanged: '',
}

const rowText: Record<DiffRowType, string> = {
  removed: 'text-red-700 dark:text-red-300',
  added: 'text-green-700 dark:text-green-300',
  unchanged: 'text-foreground',
}

const rowSign: Record<DiffRowType, string> = {
  removed: '−',
  added: '+',
  unchanged: ' ',
}

function LineNum({ n }: { n: number | null }) {
  return (
    <span className="w-10 shrink-0 select-none px-1 text-right text-subtle-foreground">
      {n ?? ''}
    </span>
  )
}

function Segments({ segments, type }: { segments: DiffSegment[]; type: DiffRowType }) {
  return (
    <>
      {segments.map((seg, i) => (
        <span
          key={i}
          className={cn(
            type === 'removed' && seg.emphasis && 'rounded-sm bg-red-500/40',
            type === 'added' && seg.emphasis && 'rounded-sm bg-green-500/40',
          )}
        >
          {seg.value}
        </span>
      ))}
    </>
  )
}

type RowWrapperProps = {
  hunkIndex?: number
  isActive?: boolean
  registerHunkRef?: (hunkIndex: number, el: HTMLElement | null) => void
  children: ReactNode
  className?: string
}

function RowWrapper({ hunkIndex, isActive, registerHunkRef, children, className }: RowWrapperProps) {
  if (hunkIndex === undefined) {
    return <div className={className}>{children}</div>
  }

  return (
    <div
      ref={(el) => registerHunkRef?.(hunkIndex, el)}
      data-diff-hunk={hunkIndex}
      className={cn(className, isActive && 'ring-2 ring-inset ring-ring')}
    >
      {children}
    </div>
  )
}

function UnifiedRowView({ row }: { row: UnifiedRow }) {
  return (
    <div className={cn('flex min-w-0', rowBg[row.type])}>
      <LineNum n={row.oldLine} />
      <LineNum n={row.newLine} />
      <span className={cn('w-5 shrink-0 select-none text-center', rowText[row.type])} aria-hidden>
        {rowSign[row.type]}
      </span>
      <span className={cn('min-w-0 flex-1 break-words pr-4', rowText[row.type])}>
        {row.type === 'removed' && <span className="sr-only">linha removida: </span>}
        {row.type === 'added' && <span className="sr-only">linha adicionada: </span>}
        <Segments segments={row.segments} type={row.type} />
      </span>
    </div>
  )
}

function SplitCellView({ cell }: { cell: SplitCell | null }) {
  if (!cell) {
    return <div className="min-h-[1.25rem] flex-1 bg-background" />
  }
  return (
    <div className={cn('flex min-w-0 flex-1', rowBg[cell.type])}>
      <LineNum n={cell.line} />
      <span className={cn('w-4 shrink-0 select-none text-center text-[11px]', rowText[cell.type])} aria-hidden>
        {rowSign[cell.type]}
      </span>
      <span className={cn('min-w-0 flex-1 break-words pr-4', rowText[cell.type])}>
        {cell.type === 'removed' && <span className="sr-only">linha removida: </span>}
        {cell.type === 'added' && <span className="sr-only">linha adicionada: </span>}
        <Segments segments={cell.segments} type={cell.type} />
      </span>
    </div>
  )
}

function SplitRowView({ row }: { row: SplitRow }) {
  return (
    <div className="flex divide-x divide-border">
      <SplitCellView cell={row.left} />
      <SplitCellView cell={row.right} />
    </div>
  )
}

function buildHunkIndexByRow(hunkStarts: number[]): Map<number, number> {
  return new Map(hunkStarts.map((rowIdx, hunkIdx) => [rowIdx, hunkIdx]))
}

export function DiffViewer({
  oldContent,
  newContent,
  oldLabel,
  newLabel,
  viewMode,
  model: modelProp,
  activeHunkIndex = null,
  registerHunkRef,
}: DiffViewerProps) {
  const computedModel = useMemo(() => computeLineDiff(oldContent, newContent), [oldContent, newContent])
  const model = modelProp ?? computedModel

  const hunkStarts = viewMode === 'unified' ? model.changeHunks.unified : model.changeHunks.split
  const hunkIndexByRow = useMemo(() => buildHunkIndexByRow(hunkStarts), [hunkStarts])

  if (!model.hasChanges) {
    return (
      <div className="flex items-center justify-center p-8 text-sm text-muted-foreground">
        As versoes selecionadas sao identicas.
      </div>
    )
  }

  return (
    <div className="font-mono text-xs leading-relaxed">
      {viewMode === 'unified' ? (
        <>
          <div
            data-diff-sticky-header
            className="sticky top-0 z-10 flex border-b border-border bg-background text-[11px] text-muted-foreground"
          >
            <span className="w-10 shrink-0 px-1 py-1 text-right">ant.</span>
            <span className="w-10 shrink-0 px-1 py-1 text-right">nov.</span>
            <span className="w-5 shrink-0" />
            <span className="flex-1 py-1 pl-1">
              {oldLabel} → {newLabel}
            </span>
          </div>
          <div className="whitespace-pre-wrap">
            {model.unified.map((row, idx) => {
              const hunkIndex = hunkIndexByRow.get(idx)
              return (
                <RowWrapper
                  key={idx}
                  hunkIndex={hunkIndex}
                  isActive={hunkIndex !== undefined && activeHunkIndex === hunkIndex}
                  registerHunkRef={registerHunkRef}
                >
                  <UnifiedRowView row={row} />
                </RowWrapper>
              )
            })}
          </div>
        </>
      ) : (
        <>
          <div
            data-diff-sticky-header
            className="sticky top-0 z-10 flex divide-x divide-border border-b border-border bg-background text-[11px] text-muted-foreground"
          >
            <span className="flex-1 px-2 py-1">{oldLabel}</span>
            <span className="flex-1 px-2 py-1">{newLabel}</span>
          </div>
          <div className="whitespace-pre-wrap">
            {model.split.map((row, idx) => {
              const hunkIndex = hunkIndexByRow.get(idx)
              return (
                <RowWrapper
                  key={idx}
                  hunkIndex={hunkIndex}
                  isActive={hunkIndex !== undefined && activeHunkIndex === hunkIndex}
                  registerHunkRef={registerHunkRef}
                >
                  <SplitRowView row={row} />
                </RowWrapper>
              )
            })}
          </div>
        </>
      )}
    </div>
  )
}