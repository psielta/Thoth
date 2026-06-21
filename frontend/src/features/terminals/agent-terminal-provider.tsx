import { useQuery } from '@tanstack/react-query'
import { useCallback, useMemo, useState } from 'react'
import { getAppSettings } from '@/api/app-settings'
import { queryKeys } from '@/api/query-keys'
import { getTerminalCapabilities } from '@/api/terminals'
import type { Prompt } from '@/api/schemas'
import { AgentTerminalContext } from './agent-terminal-context'
import { AgentTerminalDrawer } from './agent-terminal-drawer'
import { CreateAgentTerminalDialog } from './create-agent-terminal-dialog'

type OpenTarget = {
  promptId: string
  title: string
}

/**
 * Provider global (montado na raiz) que, ao criar um prompt filho, oferece abrir
 * um terminal com um agente executando o conteudo do filho. Os overlays vivem
 * aqui para sobreviver ao fechamento do drawer de geracao e manter o usuario no
 * contexto do prompt pai. Espelha o padrao do FileViewerProvider.
 */
export function AgentTerminalProvider({ children }: { children: React.ReactNode }) {
  const [pending, setPending] = useState<Prompt | null>(null)
  const [openTarget, setOpenTarget] = useState<OpenTarget | null>(null)

  const capabilitiesQuery = useQuery({
    queryKey: queryKeys.terminals.capabilities(),
    queryFn: getTerminalCapabilities,
  })
  const appSettingsQuery = useQuery({
    queryKey: queryKeys.appSettings.current(),
    queryFn: getAppSettings,
  })
  const shouldOfferAgentTerminal =
    (capabilitiesQuery.data?.enabled ?? false) &&
    appSettingsQuery.data?.showAgentTerminalOfferAfterChildPrompt === true

  const requestAgentTerminal = useCallback(
    (prompt: Prompt) => {
      // Se terminais ou a preferencia global estiverem desabilitados, a criacao
      // do filho segue normalmente sem interromper o usuario.
      if (!shouldOfferAgentTerminal) {
        return
      }

      setPending(prompt)
    },
    [shouldOfferAgentTerminal],
  )

  const value = useMemo(() => ({ requestAgentTerminal }), [requestAgentTerminal])

  return (
    <AgentTerminalContext.Provider value={value}>
      {children}
      {pending ? (
        <CreateAgentTerminalDialog
          prompt={pending}
          onCancel={() => setPending(null)}
          onCreated={(promptId) => {
            setOpenTarget({ promptId, title: pending.title })
            setPending(null)
          }}
        />
      ) : null}
      {openTarget ? (
        <AgentTerminalDrawer
          promptId={openTarget.promptId}
          title={openTarget.title}
          onClose={() => setOpenTarget(null)}
        />
      ) : null}
    </AgentTerminalContext.Provider>
  )
}
