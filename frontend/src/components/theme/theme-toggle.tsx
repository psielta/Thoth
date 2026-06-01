import { Monitor, Moon, Sun } from 'lucide-react'
import { Popover } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { useTheme, type Theme } from './theme-provider'

const OPTIONS: Array<{ value: Theme; label: string; icon: typeof Sun }> = [
  { value: 'light', label: 'Claro', icon: Sun },
  { value: 'dark', label: 'Escuro', icon: Moon },
  { value: 'system', label: 'Sistema', icon: Monitor },
]

export function ThemeToggle() {
  const { theme, resolvedTheme, setTheme } = useTheme()
  const TriggerIcon = resolvedTheme === 'dark' ? Moon : Sun

  return (
    <Popover
      ariaLabel="Alternar tema"
      className="w-44 p-1"
      triggerClassName="rounded-md focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      trigger={
        <span className="flex h-9 w-9 items-center justify-center rounded-md border border-border bg-card text-muted-foreground transition-colors hover:bg-accent hover:text-foreground">
          <TriggerIcon className="h-4 w-4" />
          <span className="sr-only">Tema atual: {resolvedTheme === 'dark' ? 'escuro' : 'claro'}</span>
        </span>
      }
    >
      <div className="grid gap-0.5">
        {OPTIONS.map((option) => {
          const Icon = option.icon
          const active = theme === option.value
          return (
            <button
              key={option.value}
              type="button"
              onClick={() => setTheme(option.value)}
              aria-pressed={active}
              className={cn(
                'flex items-center gap-2 rounded-md px-2.5 py-1.5 text-left text-sm transition-colors',
                active
                  ? 'bg-accent font-medium text-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-foreground',
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              {option.label}
            </button>
          )
        })}
      </div>
    </Popover>
  )
}
