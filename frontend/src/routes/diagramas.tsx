import { createFileRoute } from '@tanstack/react-router'
import { DiagramsPage } from '@/features/diagrams/diagrams-page'

export const Route = createFileRoute('/diagramas')({
  component: DiagramsPage,
})
