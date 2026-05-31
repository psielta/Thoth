import { forwardRef, type InputHTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export type SliderProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> & {
  label?: string
  showValue?: boolean
}

export const Slider = forwardRef<HTMLInputElement, SliderProps>(
  ({ className, label, showValue, value, ...props }, ref) => (
    <div className="flex flex-col gap-1">
      {label || showValue ? (
        <div className="flex items-center justify-between">
          {label ? <span className="text-xs text-[#66746b]">{label}</span> : null}
          {showValue ? <span className="text-xs font-medium text-[#172126]">{value}</span> : null}
        </div>
      ) : null}
      <input
        ref={ref}
        type="range"
        value={value}
        className={cn(
          'h-2 w-full cursor-pointer appearance-none rounded-full bg-[#d9dfd5]',
          '[&::-webkit-slider-thumb]:h-4 [&::-webkit-slider-thumb]:w-4',
          '[&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-full',
          '[&::-webkit-slider-thumb]:bg-[#254632] [&::-webkit-slider-thumb]:cursor-pointer',
          '[&::-moz-range-thumb]:h-4 [&::-moz-range-thumb]:w-4',
          '[&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-0',
          '[&::-moz-range-thumb]:bg-[#254632] [&::-moz-range-thumb]:cursor-pointer',
          className,
        )}
        {...props}
      />
    </div>
  ),
)

Slider.displayName = 'Slider'
