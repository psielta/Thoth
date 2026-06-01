import { useQueryClient } from '@tanstack/react-query'
import { Bot, Clock, Settings, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { getAiSettings } from '@/api/ai'
import { queryKeys } from '@/api/query-keys'
import { useQuery } from '@tanstack/react-query'
import { type AiChatSession } from '@/api/schemas'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { AiChatPanel } from './ai-chat-panel'
import { AiModelConfig, type ModelConfig } from './ai-model-config'
import { AiSessionList } from './ai-session-list'

type AiAssistantPanelProps = {
  promptId?: string
  workingDirectoryId?: string
  promptContent?: string
  onClose: () => void
}

export function AiAssistantPanel({
  promptId,
  workingDirectoryId,
  promptContent,
  onClose,
}: AiAssistantPanelProps) {
  const [activeTab, setActiveTab] = useState('chat')
  const [activeSession, setActiveSession] = useState<AiChatSession | null>(null)
  const [visible, setVisible] = useState(false)
  const queryClient = useQueryClient()

  // Animate in
  useEffect(() => {
    const t = setTimeout(() => setVisible(true), 10)
    return () => clearTimeout(t)
  }, [])

  const handleClose = () => {
    setVisible(false)
    setTimeout(onClose, 200)
  }

  const settingsQuery = useQuery({
    queryKey: queryKeys.ai.settings(),
    queryFn: getAiSettings,
  })

  const [modelConfig, setModelConfig] = useState<ModelConfig>({
    model: 'gemini-3.5-flash',
    temperature: 0.7,
    thinkingEnabled: true,
    thinkingBudget: null,
    thinkingLevel: 'high',
  })

  const settingsApplied = useRef(false)
  useEffect(() => {
    if (settingsQuery.data && !settingsApplied.current) {
      settingsApplied.current = true
      setModelConfig({
        model: settingsQuery.data.model,
        temperature: settingsQuery.data.temperature,
        thinkingEnabled: settingsQuery.data.thinkingEnabled,
        thinkingBudget: settingsQuery.data.thinkingBudget ?? null,
        thinkingLevel: settingsQuery.data.thinkingLevel ?? null,
      })
    }
  }, [settingsQuery.data])

  // Load a session from history and switch to Chat tab
  const handleSelectSession = (session: AiChatSession) => {
    setActiveSession(session)
    setActiveTab('chat')
  }

  // Start fresh: clear active session and go to Chat tab
  const handleNewSession = () => {
    setActiveSession(null)
    setActiveTab('chat')
  }

  const handleSessionDeleted = () => {
    setActiveSession(null)
    void queryClient.invalidateQueries({ queryKey: queryKeys.ai.sessions(promptId, workingDirectoryId) })
  }

  return (
    <>
      {/* Backdrop */}
      <div
        className={`fixed inset-0 z-40 bg-black/20 backdrop-blur-[1px] transition-opacity duration-200 ${
          visible ? 'opacity-100' : 'opacity-0'
        }`}
        onClick={handleClose}
      />

      {/* Drawer */}
      <div
        className={`fixed right-0 top-0 z-50 flex h-screen w-full flex-col border-l border-secondary bg-card shadow-2xl transition-transform duration-200 sm:w-[820px] lg:w-[960px] ${
          visible ? 'translate-x-0' : 'translate-x-full'
        }`}
        role="dialog"
        aria-modal="true"
        aria-label="Assistente IA"
      >
        {/* Header */}
        <div className="flex items-center justify-between border-b border-secondary bg-card px-5 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-muted">
              <Bot className="h-5 w-5 text-primary" />
            </div>
            <div>
              <p className="text-sm font-semibold text-foreground">Assistente IA</p>
              <p className="text-xs text-subtle-foreground">Engenharia de Prompts</p>
            </div>
          </div>
          <button
            onClick={handleClose}
            className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-background hover:text-foreground"
            aria-label="Fechar"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Tabs */}
        <div className="border-b border-secondary px-5 pt-3 pb-0">
          <Tabs value={activeTab} onValueChange={setActiveTab}>
            <TabsList className="gap-1">
              <TabsTrigger value="chat">
                <Bot className="mr-1.5 h-3.5 w-3.5" />
                Chat
              </TabsTrigger>
              <TabsTrigger value="history">
                <Clock className="mr-1.5 h-3.5 w-3.5" />
                Historico
              </TabsTrigger>
              <TabsTrigger value="config">
                <Settings className="mr-1.5 h-3.5 w-3.5" />
                Configuracao
              </TabsTrigger>
            </TabsList>
          </Tabs>
        </div>

        {/* Content */}
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
          {activeTab === 'chat' ? (
            <AiChatPanel
              promptId={promptId}
              workingDirectoryId={workingDirectoryId}
              promptContent={promptContent}
              modelConfig={modelConfig}
              activeSession={activeSession}
              onSessionCreated={(s) => {
                setActiveSession(s)
                void queryClient.invalidateQueries({ queryKey: queryKeys.ai.sessions(promptId, workingDirectoryId) })
              }}
              onSessionDeleted={handleSessionDeleted}
            />
          ) : activeTab === 'history' ? (
            <AiSessionList
              promptId={promptId}
              workingDirectoryId={workingDirectoryId}
              activeSessionId={activeSession?.id}
              onSelectSession={handleSelectSession}
              onNewSession={handleNewSession}
            />
          ) : (
            <div className="flex-1 overflow-y-auto p-5">
              <div className="mb-5">
                <h3 className="mb-1 text-sm font-semibold text-foreground">Modelo e parametros</h3>
                <p className="mb-4 text-xs text-muted-foreground">
                  Configuracoes usadas nas proximas sessoes de chat. Sessoes existentes
                  mantem os parametros com que foram criadas.
                </p>
                <AiModelConfig value={modelConfig} onChange={setModelConfig} />
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  )
}
