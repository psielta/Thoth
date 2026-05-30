import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import { hubUrl } from '@/env'
import { queryKeys } from '@/api/query-keys'
import { linkedDocumentSchema, promptSchema } from '@/api/schemas'

type PromptHubContextValue = {
  connected: boolean
  joinWorkingDirectory: (id: string) => void
  leaveWorkingDirectory: (id: string) => void
}

const PromptHubContext = createContext<PromptHubContextValue | null>(null)

export function PromptHubProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient()
  const connectionRef = useRef<HubConnection | null>(null)
  const joinedWorkingDirectoriesRef = useRef(new Set<string>())
  const [connected, setConnected] = useState(false)

  const invokeJoin = useCallback((id: string) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('JoinWorkingDirectory', id)
    }
  }, [])

  const invokeLeave = useCallback((id: string) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('LeaveWorkingDirectory', id)
    }
  }, [])

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build()

    connectionRef.current = connection

    connection.on('PromptCreated', (payload: unknown) => {
      const prompt = promptSchema.parse(payload)
      queryClient.setQueryData(queryKeys.prompts.detail(prompt.id), prompt)
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
    })

    connection.on('PromptUpdated', (payload: unknown) => {
      const prompt = promptSchema.parse(payload)
      queryClient.setQueryData(queryKeys.prompts.detail(prompt.id), prompt)
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.versions(prompt.id) })
    })

    connection.on('PromptDeleted', (promptId: string) => {
      queryClient.removeQueries({ queryKey: queryKeys.prompts.detail(promptId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
    })

    connection.on('LinkedDocumentLinked', (payload: unknown) => {
      const document = linkedDocumentSchema.parse(payload)
      queryClient.setQueryData(queryKeys.linkedDocuments.detail(document.id), document)
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(document.promptId) })
    })

    connection.on('LinkedDocumentUpdated', (payload: unknown) => {
      const document = linkedDocumentSchema.parse(payload)
      queryClient.setQueryData(queryKeys.linkedDocuments.detail(document.id), document)
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(document.promptId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.contentRoot(document.id) })
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.versions(document.id) })
    })

    connection.on('LinkedDocumentRemoved', (linkedDocumentId: string, promptId: string) => {
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.detail(linkedDocumentId) })
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.contentRoot(linkedDocumentId) })
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.versions(linkedDocumentId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(promptId) })
    })

    connection.onreconnecting(() => setConnected(false))
    connection.onreconnected(() => {
      setConnected(true)
      joinedWorkingDirectoriesRef.current.forEach(invokeJoin)
    })
    connection.onclose(() => setConnected(false))

    void connection
      .start()
      .then(() => {
        setConnected(true)
        joinedWorkingDirectoriesRef.current.forEach(invokeJoin)
      })
      .catch(() => setConnected(false))

    return () => {
      connectionRef.current = null
      void connection.stop()
    }
  }, [invokeJoin, queryClient])

  useEffect(() => {
    if (!connected) {
      return
    }

    joinedWorkingDirectoriesRef.current.forEach(invokeJoin)
  }, [connected, invokeJoin])

  const joinWorkingDirectory = useCallback(
    (id: string) => {
      joinedWorkingDirectoriesRef.current.add(id)
      invokeJoin(id)
    },
    [invokeJoin],
  )

  const leaveWorkingDirectory = useCallback(
    (id: string) => {
      joinedWorkingDirectoriesRef.current.delete(id)
      invokeLeave(id)
    },
    [invokeLeave],
  )

  const value = useMemo(
    () => ({
      connected,
      joinWorkingDirectory,
      leaveWorkingDirectory,
    }),
    [connected, joinWorkingDirectory, leaveWorkingDirectory],
  )

  return <PromptHubContext.Provider value={value}>{children}</PromptHubContext.Provider>
}

export function usePromptHub() {
  const context = useContext(PromptHubContext)
  if (!context) {
    throw new Error('usePromptHub must be used within PromptHubProvider')
  }

  return context
}
