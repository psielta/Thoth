import { createFileRoute } from '@tanstack/react-router'
import { NotebooksPage } from '@/features/notebooks/notebooks-page'

export const Route = createFileRoute('/notas')({
  component: NotebooksPage,
})
