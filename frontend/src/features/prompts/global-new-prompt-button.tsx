import { useQuery } from '@tanstack/react-query'
import { useParams } from '@tanstack/react-router'
import { Plus } from 'lucide-react'
import { useState } from 'react'
import { queryKeys } from '@/api/query-keys'
import { listWorkingDirectories } from '@/api/working-directories'
import { Button } from '@/components/ui/button'
import { NewPromptDrawer } from '@/features/workflow/new-prompt-drawer'

/**
 * Floating action button disponivel em todas as rotas para abrir o drawer de
 * novo prompt sem depender da tela atual. Quando a rota atual pertence a um
 * workspace, ele vira o diretorio pre-selecionado do formulario.
 */
export function GlobalNewPromptButton() {
  const [open, setOpen] = useState(false)
  const params = useParams({ strict: false }) as { workspaceId?: string }

  const workspacesQuery = useQuery({
    queryKey: queryKeys.workingDirectories.all,
    queryFn: listWorkingDirectories,
    enabled: open,
  })

  return (
    <>
      {!open ? (
        <Button
          type="button"
          size="icon"
          onClick={() => setOpen(true)}
          title="Novo prompt"
          aria-label="Novo prompt"
          className="fixed bottom-5 right-5 z-40 h-12 w-12 rounded-full shadow-lg sm:bottom-6 sm:right-6"
        >
          <Plus className="h-5 w-5" />
        </Button>
      ) : null}

      {open && workspacesQuery.data ? (
        <NewPromptDrawer
          defaultWorkingDirectoryId={params.workspaceId}
          workspaces={workspacesQuery.data}
          onClose={() => setOpen(false)}
          onCreated={() => setOpen(false)}
        />
      ) : null}
    </>
  )
}
