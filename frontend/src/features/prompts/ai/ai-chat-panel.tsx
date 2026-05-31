import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Bot, Loader2, Send, Trash2 } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { deleteChatSession, startChatSession } from '@/api/ai'
import { queryKeys } from '@/api/query-keys'
import { type AiChatSession } from '@/api/schemas'
import { getErrorMessage } from '@/api/client'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { type ModelConfig } from './ai-model-config'
import { useChatStream, type ChatStreamMessage } from './use-chat-stream'

type AiChatPanelProps = {
  promptId?: string
  workingDirectoryId?: string
  promptContent?: string
  modelConfig: ModelConfig
  activeSession: AiChatSession | null
  onSessionCreated: (session: AiChatSession) => void
  onSessionDeleted: () => void
}

export function AiChatPanel({
  promptId,
  workingDirectoryId,
  promptContent,
  modelConfig,
  activeSession,
  onSessionCreated,
  onSessionDeleted,
}: AiChatPanelProps) {
  const queryClient = useQueryClient()
  const [input, setInput] = useState('')
  const [includeContext, setIncludeContext] = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const { messages, isStreaming, error, sendMessage, initMessages } = useChatStream()

  useEffect(() => {
    if (activeSession?.messages) {
      initMessages(
        activeSession.messages.map((m) => ({
          id: m.id,
          role: m.role as 'user' | 'model',
          content: m.content,
          cachedTokens: m.cachedTokens,
        })),
      )
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- intentionally run only when session id changes
  }, [activeSession?.id, initMessages])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const startSessionMutation = useMutation({
    mutationFn: () =>
      startChatSession({
        title: 'Chat',
        promptId,
        workingDirectoryId,
        model: modelConfig.model,
        temperature: modelConfig.temperature,
        thinkingEnabled: modelConfig.thinkingEnabled,
        thinkingBudget: modelConfig.thinkingBudget,
        thinkingLevel: modelConfig.thinkingLevel,
      }),
    onSuccess: (session) => {
      onSessionCreated(session)
      void queryClient.invalidateQueries({ queryKey: queryKeys.ai.sessions(promptId) })
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  })

  const deleteSessionMutation = useMutation({
    mutationFn: () => deleteChatSession(activeSession!.id),
    onSuccess: () => {
      onSessionDeleted()
      void queryClient.invalidateQueries({ queryKey: queryKeys.ai.sessions(promptId) })
      toast.success('Sessao removida.')
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  })

  const handleSend = async () => {
    const text = input.trim()
    if (!text || isStreaming) return

    let sessionId = activeSession?.id
    if (!sessionId) {
      const session = await startSessionMutation.mutateAsync()
      sessionId = session.id
    }

    setInput('')
    await sendMessage({
      sessionId,
      text,
      includePromptContext: includeContext,
      promptContent,
    })
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      void handleSend()
    }
  }

  const noMessages = messages.length === 0

  return (
    <div className="flex h-full flex-col gap-3">
      {/* Header */}
      {activeSession ? (
        <div className="flex items-center justify-between rounded-md border border-[#d9dfd5] bg-[#f8faf7] px-3 py-2">
          <div className="flex items-center gap-2 text-xs text-[#66746b]">
            <Bot className="h-3.5 w-3.5" />
            <span>{activeSession.title}</span>
            <span className="text-[#d9dfd5]">·</span>
            <span>{activeSession.model}</span>
          </div>
          <button
            onClick={() => deleteSessionMutation.mutate()}
            disabled={deleteSessionMutation.isPending}
            className="rounded p-1 text-[#66746b] hover:bg-white hover:text-[#b42318]"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        </div>
      ) : null}

      {/* Messages */}
      <div className="flex-1 overflow-y-auto">
        {noMessages ? (
          <div className="flex h-full flex-col items-center justify-center gap-2 text-center text-sm text-[#66746b]">
            <Bot className="h-8 w-8 text-[#d9dfd5]" />
            <p>Inicie uma conversa com o assistente IA.</p>
            {promptContent ? (
              <p className="text-xs">Ative &quot;Incluir contexto do prompt&quot; para enviar o conteudo atual.</p>
            ) : null}
          </div>
        ) : (
          <div className="flex flex-col gap-3 pb-2">
            {messages.map((msg) => (
              <ChatMessage key={msg.id} message={msg} />
            ))}
            {isStreaming ? (
              <div className="flex items-center gap-2 text-xs text-[#66746b]">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Gerando...
              </div>
            ) : null}
            {error ? (
              <div className="rounded-md border border-[#f8b4aa] bg-[#fff3f0] px-3 py-2 text-xs text-[#8a241b]">
                {error}
              </div>
            ) : null}
            <div ref={messagesEndRef} />
          </div>
        )}
      </div>

      {/* Input */}
      <div className="flex flex-col gap-2 rounded-md border border-[#d9dfd5] bg-white p-2">
        {promptContent ? (
          <Switch
            id="include-context"
            checked={includeContext}
            onChange={(e) => setIncludeContext(e.target.checked)}
            label="Incluir contexto do prompt"
          />
        ) : null}
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Envie uma mensagem... (Enter para enviar, Shift+Enter para nova linha)"
          rows={3}
          className="w-full resize-none rounded-md border-0 bg-transparent text-sm text-[#172126] placeholder:text-[#66746b] focus:outline-none"
          disabled={isStreaming || startSessionMutation.isPending}
        />
        <div className="flex justify-end">
          <Button
            type="button"
            size="sm"
            onClick={() => void handleSend()}
            disabled={!input.trim() || isStreaming || startSessionMutation.isPending}
          >
            {isStreaming || startSessionMutation.isPending ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              <Send className="h-3.5 w-3.5" />
            )}
            Enviar
          </Button>
        </div>
      </div>
    </div>
  )
}

function ChatMessage({ message }: { message: ChatStreamMessage }) {
  const isUser = message.role === 'user'

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[85%] rounded-lg px-3 py-2 text-sm ${
          isUser
            ? 'bg-[#254632] text-white'
            : 'border border-[#d9dfd5] bg-white text-[#172126]'
        }`}
      >
        {message.thought ? (
          <details className="mb-2">
            <summary className="cursor-pointer text-xs opacity-70">Ver raciocinio</summary>
            <div className="mt-1 border-l-2 border-current pl-2 text-xs opacity-70">
              {message.thought}
            </div>
          </details>
        ) : null}
        <div className="whitespace-pre-wrap">{message.content}</div>
        {message.cachedTokens ? (
          <div className="mt-1 text-right text-xs opacity-50">
            {message.cachedTokens} tokens em cache
          </div>
        ) : null}
      </div>
    </div>
  )
}
