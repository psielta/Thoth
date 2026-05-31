import { useQuery } from '@tanstack/react-query'
import { Bot, Settings, X } from 'lucide-react'
import { useState } from 'react'
import { getAiSettings } from '@/api/ai'
import { queryKeys } from '@/api/query-keys'
import { type AiChatSession } from '@/api/schemas'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { AiChatPanel } from './ai-chat-panel'
import { AiModelConfig, type ModelConfig } from './ai-model-config'

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

  const settingsQuery = useQuery({
    queryKey: queryKeys.ai.settings(),
    queryFn: getAiSettings,
  })

  const [modelConfig, setModelConfig] = useState<ModelConfig>({
    model: settingsQuery.data?.model ?? 'gemini-2.5-flash',
    temperature: settingsQuery.data?.temperature ?? 0.7,
    thinkingEnabled: settingsQuery.data?.thinkingEnabled ?? false,
    thinkingBudget: settingsQuery.data?.thinkingBudget ?? null,
    thinkingLevel: settingsQuery.data?.thinkingLevel ?? null,
  })

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-[#d9dfd5] px-4 py-3">
        <div className="flex items-center gap-2">
          <Bot className="h-5 w-5 text-[#254632]" />
          <span className="text-sm font-semibold text-[#172126]">Assistente IA</span>
        </div>
        <button
          onClick={onClose}
          className="rounded-md p-1 text-[#66746b] hover:bg-[#eef2eb] hover:text-[#172126]"
        >
          <X className="h-4 w-4" />
        </button>
      </div>

      {/* Tabs */}
      <div className="border-b border-[#d9dfd5] px-4 py-2">
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="w-full">
            <TabsTrigger value="chat">Chat</TabsTrigger>
            <TabsTrigger value="config">
              <Settings className="mr-1 h-3 w-3" />
              Configuracao
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-hidden p-4">
        {activeTab === 'chat' ? (
          <AiChatPanel
            promptId={promptId}
            workingDirectoryId={workingDirectoryId}
            promptContent={promptContent}
            modelConfig={modelConfig}
            activeSession={activeSession}
            onSessionCreated={setActiveSession}
            onSessionDeleted={() => setActiveSession(null)}
          />
        ) : (
          <div className="flex flex-col gap-4">
            <div>
              <h3 className="mb-2 text-sm font-medium text-[#172126]">Modelo e parametros</h3>
              <AiModelConfig value={modelConfig} onChange={setModelConfig} />
            </div>
            <p className="text-xs text-[#66746b]">
              As configuracoes serao usadas nas proximas sessoes de chat. Sessoes existentes
              mantem os parametros com que foram criadas.
            </p>
          </div>
        )}
      </div>
    </div>
  )
}
