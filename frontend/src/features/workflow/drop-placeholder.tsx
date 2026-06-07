import { MoveRight } from 'lucide-react'
import type { DropPlaceholderLayout } from './drop-placeholder-state'

export function DropPlaceholder({ layout }: { layout: DropPlaceholderLayout }) {
  return (
    <div
      role="status"
      aria-label="Destino do card"
      className={`pointer-events-none grid min-h-28 place-items-center rounded-lg border-2 border-dashed border-primary bg-primary/10 px-3 py-4 text-sm font-medium text-primary ${
        layout === 'vertical' ? 'w-full' : ''
      }`}
    >
      <span className="flex items-center gap-2">
        <MoveRight className="h-4 w-4" />
        Solte aqui
      </span>
    </div>
  )
}
