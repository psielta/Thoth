export type DropPlaceholderLayout = 'kanban' | 'vertical'

export type DropTarget = {
  columnId: string
  index: number
}

export type DropPlaceholderState = {
  columnId: string
  acceptsDrop: boolean
  draggedPromptId: string | null
  dropTarget: DropTarget | null
  placeholderIndex: number
  isMoving?: boolean
}

export function shouldShowDropPlaceholder({
  columnId,
  acceptsDrop,
  draggedPromptId,
  dropTarget,
  placeholderIndex,
  isMoving = false,
}: DropPlaceholderState) {
  return (
    acceptsDrop &&
    !isMoving &&
    Boolean(draggedPromptId) &&
    dropTarget?.columnId === columnId &&
    dropTarget.index === placeholderIndex
  )
}

export function computeReorderedIds(columnIds: string[], draggedId: string, dropIndex: number) {
  const currentIndex = columnIds.indexOf(draggedId)
  if (currentIndex < 0) {
    return columnIds
  }

  const normalizedDropIndex = Math.max(0, Math.min(dropIndex, columnIds.length))
  const insertionIndex = currentIndex < normalizedDropIndex ? normalizedDropIndex - 1 : normalizedDropIndex
  const next = columnIds.filter((id) => id !== draggedId)
  next.splice(Math.max(0, Math.min(insertionIndex, next.length)), 0, draggedId)
  return next
}
