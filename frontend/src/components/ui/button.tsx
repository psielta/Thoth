import { cva, type VariantProps } from 'class-variance-authority'
import type * as React from 'react'
import { cn } from '@/lib/utils'

const buttonVariants = cva(
  'inline-flex h-9 shrink-0 items-center justify-center gap-2 rounded-md border text-sm font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'border-[#254632] bg-[#254632] text-white hover:bg-[#1c3727] focus-visible:outline-[#254632]',
        secondary: 'border-[#cbd5c8] bg-white text-[#172126] hover:bg-[#eef2eb] focus-visible:outline-[#5e7461]',
        ghost: 'border-transparent bg-transparent text-[#253035] hover:bg-[#e7ece6] focus-visible:outline-[#5e7461]',
        destructive: 'border-[#b42318] bg-[#b42318] text-white hover:bg-[#971d14] focus-visible:outline-[#b42318]',
      },
      size: {
        default: 'px-3.5',
        sm: 'h-8 px-2.5 text-xs',
        icon: 'h-9 w-9 p-0',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
)

export type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants>

export function Button({ className, variant, size, ...props }: ButtonProps) {
  return <button className={cn(buttonVariants({ variant, size }), className)} {...props} />
}
