import { cva, type VariantProps } from 'class-variance-authority'
import type * as React from 'react'
import { cn } from '@/lib/utils'

const badgeVariants = cva('inline-flex items-center rounded-md px-2 py-1 text-xs font-medium', {
  variants: {
    variant: {
      neutral: 'bg-muted text-muted-foreground',
      green: 'bg-success-soft text-success-foreground',
      amber: 'bg-warning-soft text-warning-foreground',
      blue: 'bg-info-soft text-info-foreground',
      red: 'bg-danger-soft text-danger-soft-foreground',
    },
  },
  defaultVariants: {
    variant: 'neutral',
  },
})

export type BadgeProps = React.HTMLAttributes<HTMLSpanElement> & VariantProps<typeof badgeVariants>

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant }), className)} {...props} />
}
