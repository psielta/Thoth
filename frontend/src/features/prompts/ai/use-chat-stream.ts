import { useCallback, useRef, useState } from 'react'
import { streamChatMessage } from '@/api/ai'

export type ChatStreamMessage = {
  id: string
  role: 'user' | 'model'
  content: string
  thought?: string
  cachedTokens?: number | null
}

export function useChatStream() {
  const [messages, setMessages] = useState<ChatStreamMessage[]>([])
  const [isStreaming, setIsStreaming] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const abortRef = useRef<AbortController | null>(null)

  const sendMessage = useCallback(
    async (params: {
      sessionId: string
      text: string
      includePromptContext: boolean
      promptContent?: string
    }) => {
      if (isStreaming) return

      const userMsg: ChatStreamMessage = {
        id: crypto.randomUUID(),
        role: 'user',
        content: params.text,
      }

      setMessages((prev) => [...prev, userMsg])
      setIsStreaming(true)
      setError(null)

      const modelMsgId = crypto.randomUUID()
      setMessages((prev) => [
        ...prev,
        { id: modelMsgId, role: 'model', content: '', thought: '' },
      ])

      const controller = new AbortController()
      abortRef.current = controller

      try {
        for await (const chunk of streamChatMessage({
          sessionId: params.sessionId,
          message: params.text,
          includePromptContext: params.includePromptContext,
          promptContent: params.promptContent,
          signal: controller.signal,
        })) {
          if (chunk.done) {
            setMessages((prev) =>
              prev.map((m) =>
                m.id === modelMsgId ? { ...m, cachedTokens: chunk.cachedTokens } : m,
              ),
            )
            break
          }

          setMessages((prev) =>
            prev.map((m) => {
              if (m.id !== modelMsgId) return m
              if (chunk.isThought) {
                return { ...m, thought: (m.thought ?? '') + chunk.text }
              }
              return { ...m, content: m.content + chunk.text }
            }),
          )
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Erro ao enviar mensagem.')
        setMessages((prev) => prev.filter((m) => m.id !== modelMsgId))
      } finally {
        if (abortRef.current === controller) {
          abortRef.current = null
        }
        setIsStreaming(false)
      }
    },
    [isStreaming],
  )

  const reset = useCallback(() => {
    abortRef.current?.abort()
    setMessages([])
    setError(null)
    setIsStreaming(false)
  }, [])

  const initMessages = useCallback((msgs: ChatStreamMessage[]) => {
    setMessages(msgs)
  }, [])

  return { messages, isStreaming, error, sendMessage, reset, initMessages }
}
