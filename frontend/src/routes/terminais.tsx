import { createFileRoute } from '@tanstack/react-router'
import { TerminalsPage } from '@/features/terminals/terminals-page'

export const Route = createFileRoute('/terminais')({
  component: TerminalsPage,
})
