import { createFileRoute } from '@tanstack/react-router'
import { Board } from '@/features/workflow/board'

export const Route = createFileRoute('/')({
  component: Board,
})
