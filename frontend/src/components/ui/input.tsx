import { forwardRef, type InputHTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      className={cn(
        'h-9 w-full rounded-md border border-[#cbd5c8] bg-white px-3 text-sm text-[#172126] outline-none transition-colors placeholder:text-[#66746b] focus:border-[#5e7461] focus:ring-2 focus:ring-[#5e7461]/20 disabled:cursor-not-allowed disabled:bg-[#eef2eb]',
        className,
      )}
      {...props}
    />
  ),
)

Input.displayName = 'Input'
