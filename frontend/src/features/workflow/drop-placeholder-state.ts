export type DropPlaceholderLayout = 'kanban' | 'vertical'

export type DropPlaceholderState = {
  columnId: string
  droppable: boolean
  draggedPromptId: string | null
  dragOverColumnId: string | null
  isMoving?: boolean
}

export function shouldShowDropPlaceholder({
  columnId,
  droppable,
  draggedPromptId,
  dragOverColumnId,
  isMoving = false,
}: DropPlaceholderState) {
  return droppable && !isMoving && Boolean(draggedPromptId) && dragOverColumnId === columnId
}
