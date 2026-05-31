import { createContext, useContext, type ReactNode } from 'react'
import { cn } from '@/lib/utils'

type TabsContextValue = {
  activeTab: string
  onChange: (tab: string) => void
}

const TabsContext = createContext<TabsContextValue>({ activeTab: '', onChange: () => {} })

export function Tabs({
  value,
  onValueChange,
  children,
  className,
}: {
  value: string
  onValueChange: (v: string) => void
  children: ReactNode
  className?: string
}) {
  return (
    <TabsContext.Provider value={{ activeTab: value, onChange: onValueChange }}>
      <div className={className}>{children}</div>
    </TabsContext.Provider>
  )
}

export function TabsList({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div
      role="tablist"
      className={cn('flex rounded-md border border-[#d9dfd5] bg-[#f5f7f4] p-0.5', className)}
    >
      {children}
    </div>
  )
}

export function TabsTrigger({
  value,
  children,
  className,
}: {
  value: string
  children: ReactNode
  className?: string
}) {
  const { activeTab, onChange } = useContext(TabsContext)
  const active = activeTab === value

  return (
    <button
      role="tab"
      aria-selected={active}
      onClick={() => onChange(value)}
      className={cn(
        'flex-1 rounded px-3 py-1 text-sm font-medium transition-colors',
        active
          ? 'bg-white text-[#172126] shadow-sm'
          : 'text-[#66746b] hover:text-[#172126]',
        className,
      )}
    >
      {children}
    </button>
  )
}

export function TabsContent({
  value,
  children,
  className,
}: {
  value: string
  children: ReactNode
  className?: string
}) {
  const { activeTab } = useContext(TabsContext)
  if (activeTab !== value) return null
  return <div className={className}>{children}</div>
}
