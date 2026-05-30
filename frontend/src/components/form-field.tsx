import type { ReactNode } from 'react'
import { Label } from '@/components/ui/label'

type FormFieldProps = {
  label: string
  htmlFor?: string
  error?: string
  children: ReactNode
}

export function FormField({ label, htmlFor, error, children }: FormFieldProps) {
  return (
    <div className="grid gap-1.5">
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
      {error ? <p className="text-xs font-medium text-[#b42318]">{error}</p> : null}
    </div>
  )
}
