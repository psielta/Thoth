import { forwardRef, type InputHTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export type SwitchProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> & {
  label?: string
}

export const Switch = forwardRef<HTMLInputElement, SwitchProps>(
  ({ className, label, id, checked, ...props }, ref) => (
    <label htmlFor={id} className="flex cursor-pointer items-center gap-2">
      <div className="relative">
        <input
          ref={ref}
          id={id}
          type="checkbox"
          checked={checked}
          className="peer sr-only"
          {...props}
        />
        <div
          className={cn(
            'h-5 w-9 rounded-full border border-[#d9dfd5] bg-[#eef2eb] transition-colors',
            'peer-checked:border-[#254632] peer-checked:bg-[#254632]',
            'peer-focus-visible:outline peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-[#254632]',
            className,
          )}
        />
        <div
          className={cn(
            'absolute left-0.5 top-0.5 h-4 w-4 rounded-full bg-[#66746b] transition-transform',
            'peer-checked:translate-x-4 peer-checked:bg-white',
          )}
        />
      </div>
      {label ? <span className="text-sm text-[#172126]">{label}</span> : null}
    </label>
  ),
)

Switch.displayName = 'Switch'
