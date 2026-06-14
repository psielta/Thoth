import { useMutation } from '@tanstack/react-query'
import { Code2, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { openWorkingDirectoryInVsCode } from '@/api/working-directories'
import { Button, type ButtonProps } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type OpenVsCodeButtonProps = {
  workingDirectoryId: string
  workspaceName?: string
  iconOnly?: boolean
  className?: string
  variant?: ButtonProps['variant']
  size?: ButtonProps['size']
}

export function OpenVsCodeButton({
  workingDirectoryId,
  workspaceName,
  iconOnly = false,
  className,
  variant = 'secondary',
  size = iconOnly ? 'icon' : 'sm',
}: OpenVsCodeButtonProps) {
  const mutation = useMutation({
    mutationFn: () => openWorkingDirectoryInVsCode(workingDirectoryId),
    onSuccess: () => toast.success('VS Code aberto para o workspace.'),
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const label = workspaceName ? `Abrir ${workspaceName} no VS Code` : 'Abrir workspace no VS Code'

  return (
    <Button
      type="button"
      variant={variant}
      size={size}
      className={cn(iconOnly ? 'h-8 w-8' : null, className)}
      onClick={(event) => {
        event.stopPropagation()
        mutation.mutate()
      }}
      disabled={mutation.isPending}
      title={label}
      aria-label={label}
    >
      {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Code2 className="h-4 w-4" />}
      {iconOnly ? null : 'VS Code'}
    </Button>
  )
}
