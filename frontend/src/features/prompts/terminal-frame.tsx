import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

export type TerminalFrameVariant = 'prompt' | 'drawer' | 'global'

type TerminalFrameProps = {
  variant: TerminalFrameVariant
  children: ReactNode
  className?: string
}

const variantClasses: Record<TerminalFrameVariant, string> = {
  prompt: 'h-[min(70vh,640px)]',
  drawer: 'h-full',
  global: 'h-[min(64vh,560px)]',
}

export function TerminalFrame({ variant, children, className }: TerminalFrameProps) {
  return (
    <div
      className={cn(
        'relative min-h-0 w-full overflow-hidden rounded-md border border-border bg-[#0f1117]',
        variantClasses[variant],
        className,
      )}
    >
      {children}
    </div>
  )
}
