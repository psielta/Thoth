import { cva, type VariantProps } from 'class-variance-authority'
import type * as React from 'react'
import { cn } from '@/lib/utils'

const badgeVariants = cva('inline-flex items-center rounded-md px-2 py-1 text-xs font-medium', {
  variants: {
    variant: {
      neutral: 'bg-[#eef2eb] text-[#425048]',
      green: 'bg-[#e1f2e1] text-[#215631]',
      amber: 'bg-[#fff0c2] text-[#6b4d00]',
      blue: 'bg-[#e0eefb] text-[#234c71]',
      red: 'bg-[#ffe4df] text-[#8a241b]',
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
