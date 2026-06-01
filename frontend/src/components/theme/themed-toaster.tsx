import { Toaster } from 'sonner'
import { useTheme } from './theme-provider'

/** Sonner toaster that follows the app theme. */
export function ThemedToaster() {
  const { resolvedTheme } = useTheme()
  return <Toaster richColors position="bottom-right" theme={resolvedTheme} />
}
