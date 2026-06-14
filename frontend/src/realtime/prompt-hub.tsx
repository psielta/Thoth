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
import {
  agentUsageSchema,
  linkedDocumentSchema,
  promptSchema,
  taskSummarySchema,
} from '@/api/schemas'
import { fileSubscriptionKey, parentDirectoryPath } from '@/features/files/file-key'

type FileSubscription = {
  workingDirectoryId: string
  relativePath: string
  count: number
}

type TerminalOutputHandler = (startOffset: number, dataBase64: string) => void
type TerminalExitHandler = (exitCode: number) => void

type PromptHubContextValue = {
  connected: boolean
  joinWorkingDirectory: (id: string) => void
  leaveWorkingDirectory: (id: string) => void
  joinTasks: () => void
  leaveTasks: () => void
  joinFile: (workingDirectoryId: string, relativePath: string) => void
  leaveFile: (workingDirectoryId: string, relativePath: string) => void
  joinTerminal: (sessionId: string) => void
  leaveTerminal: (sessionId: string) => void
  sendTerminalInput: (sessionId: string, dataBase64: string) => void
  resizeTerminal: (sessionId: string, cols: number, rows: number) => void
  subscribeTerminalOutput: (sessionId: string, handler: TerminalOutputHandler) => () => void
  subscribeTerminalExit: (sessionId: string, handler: TerminalExitHandler) => () => void
}

const PromptHubContext = createContext<PromptHubContextValue | null>(null)

export function PromptHubProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient()
  const connectionRef = useRef<HubConnection | null>(null)
  const joinedWorkingDirectoriesRef = useRef(new Set<string>())
  const joinedFilesRef = useRef(new Map<string, FileSubscription>())
  const joinedTerminalsRef = useRef(new Set<string>())
  const terminalOutputHandlersRef = useRef(new Map<string, Set<TerminalOutputHandler>>())
  const terminalExitHandlersRef = useRef(new Map<string, Set<TerminalExitHandler>>())
  const tasksJoinedRef = useRef(false)
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

  const invokeJoinTasks = useCallback(() => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('JoinTasks')
    }
  }, [])

  const invokeJoinFile = useCallback((workingDirectoryId: string, relativePath: string) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('JoinFile', workingDirectoryId, relativePath).catch(() => {
        joinedFilesRef.current.delete(fileSubscriptionKey(workingDirectoryId, relativePath))
      })
    }
  }, [])

  const invokeLeaveFile = useCallback((workingDirectoryId: string, relativePath: string) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('LeaveFile', workingDirectoryId, relativePath).catch(() => undefined)
    }
  }, [])

  const invokeJoinTerminal = useCallback((sessionId: string) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('JoinTerminal', sessionId).catch(() => {
        joinedTerminalsRef.current.delete(sessionId)
      })
    }
  }, [])

  const invokeLeaveTerminal = useCallback((sessionId: string) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('LeaveTerminal', sessionId).catch(() => undefined)
    }
  }, [])

  const rejoinAll = useCallback(() => {
    joinedWorkingDirectoriesRef.current.forEach(invokeJoin)
    if (tasksJoinedRef.current) {
      invokeJoinTasks()
    }
    joinedFilesRef.current.forEach(({ workingDirectoryId, relativePath }) => {
      invokeJoinFile(workingDirectoryId, relativePath)
    })
    joinedTerminalsRef.current.forEach(invokeJoinTerminal)
  }, [invokeJoin, invokeJoinFile, invokeJoinTasks, invokeJoinTerminal])

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
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
    })

    connection.on('PromptUpdated', (payload: unknown) => {
      const prompt = promptSchema.parse(payload)
      queryClient.setQueryData(queryKeys.prompts.detail(prompt.id), prompt)
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.versions(prompt.id) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
    })

    connection.on('PromptDeleted', (promptId: string) => {
      queryClient.removeQueries({ queryKey: queryKeys.prompts.detail(promptId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
    })

    connection.on('LinkedDocumentLinked', (payload: unknown) => {
      const document = linkedDocumentSchema.parse(payload)
      queryClient.setQueryData(queryKeys.linkedDocuments.detail(document.id), document)
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(document.promptId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
    })

    connection.on('LinkedDocumentUpdated', (payload: unknown) => {
      const document = linkedDocumentSchema.parse(payload)
      queryClient.setQueryData(queryKeys.linkedDocuments.detail(document.id), document)
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(document.promptId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.contentRoot(document.id) })
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.versions(document.id) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
    })

    connection.on('LinkedDocumentRemoved', (linkedDocumentId: string, promptId: string) => {
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.detail(linkedDocumentId) })
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.contentRoot(linkedDocumentId) })
      queryClient.removeQueries({ queryKey: queryKeys.linkedDocuments.versions(linkedDocumentId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(promptId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
    })

    connection.on('TaskWorkflowChanged', (payload: unknown) => {
      const summary = taskSummarySchema.parse(payload)
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.workflow.detail(summary.promptId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
    })

    connection.on('AgentUsageUpdated', (payload: unknown) => {
      const usage = agentUsageSchema.parse(payload)
      queryClient.setQueryData(queryKeys.agentUsage.current(), usage)
    })

    connection.on('TerminalOutput', (sessionId: string, startOffset: number, dataBase64: string) => {
      const handlers = terminalOutputHandlersRef.current.get(sessionId)
      handlers?.forEach((handler) => handler(startOffset, dataBase64))
    })

    connection.on('TerminalExited', (sessionId: string, exitCode: number) => {
      const handlers = terminalExitHandlersRef.current.get(sessionId)
      handlers?.forEach((handler) => handler(exitCode))
    })

    connection.on('WorkspaceFileChanged', (workingDirectoryId: string, changedPath: string) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.git.status(workingDirectoryId) })
      // File notifications are emitted only for joined file groups, so Git cache coverage is partial.
      queryClient.invalidateQueries({ queryKey: queryKeys.git.diff(workingDirectoryId, changedPath) })

      const subscription = joinedFilesRef.current.get(fileSubscriptionKey(workingDirectoryId, changedPath))
      if (!subscription) {
        return
      }

      queryClient.invalidateQueries({
        queryKey: queryKeys.files.content(subscription.workingDirectoryId, subscription.relativePath),
      })
      queryClient.invalidateQueries({
        queryKey: queryKeys.files.tree(
          subscription.workingDirectoryId,
          parentDirectoryPath(subscription.relativePath),
        ),
      })
    })

    connection.onreconnecting(() => setConnected(false))
    connection.onreconnected(() => {
      setConnected(true)
      rejoinAll()
    })
    connection.onclose(() => setConnected(false))

    void connection
      .start()
      .then(() => {
        setConnected(true)
        rejoinAll()
      })
      .catch(() => setConnected(false))

    return () => {
      connectionRef.current = null
      void connection.stop()
    }
  }, [queryClient, rejoinAll])

  useEffect(() => {
    if (!connected) {
      return
    }

    rejoinAll()
  }, [connected, rejoinAll])

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

  const joinTasks = useCallback(() => {
    tasksJoinedRef.current = true
    invokeJoinTasks()
  }, [invokeJoinTasks])

  const leaveTasks = useCallback(() => {
    tasksJoinedRef.current = false
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('LeaveTasks')
    }
  }, [])

  const joinFile = useCallback(
    (workingDirectoryId: string, relativePath: string) => {
      const key = fileSubscriptionKey(workingDirectoryId, relativePath)
      const existing = joinedFilesRef.current.get(key)
      if (existing) {
        existing.count += 1
        return
      }

      joinedFilesRef.current.set(key, { workingDirectoryId, relativePath, count: 1 })
      invokeJoinFile(workingDirectoryId, relativePath)
    },
    [invokeJoinFile],
  )

  const leaveFile = useCallback(
    (workingDirectoryId: string, relativePath: string) => {
      const key = fileSubscriptionKey(workingDirectoryId, relativePath)
      const existing = joinedFilesRef.current.get(key)
      if (!existing) {
        return
      }

      if (existing.count <= 1) {
        joinedFilesRef.current.delete(key)
        invokeLeaveFile(workingDirectoryId, relativePath)
        return
      }

      existing.count -= 1
    },
    [invokeLeaveFile],
  )

  const joinTerminal = useCallback(
    (sessionId: string) => {
      joinedTerminalsRef.current.add(sessionId)
      invokeJoinTerminal(sessionId)
    },
    [invokeJoinTerminal],
  )

  const leaveTerminal = useCallback(
    (sessionId: string) => {
      joinedTerminalsRef.current.delete(sessionId)
      invokeLeaveTerminal(sessionId)
    },
    [invokeLeaveTerminal],
  )

  const sendTerminalInput = useCallback((sessionId: string, dataBase64: string) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('SendTerminalInput', sessionId, dataBase64)
    }
  }, [])

  const resizeTerminal = useCallback((sessionId: string, cols: number, rows: number) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      void connection.invoke('ResizeTerminal', sessionId, cols, rows)
    }
  }, [])

  const subscribeTerminalOutput = useCallback((sessionId: string, handler: TerminalOutputHandler) => {
    const handlers = terminalOutputHandlersRef.current.get(sessionId) ?? new Set<TerminalOutputHandler>()
    handlers.add(handler)
    terminalOutputHandlersRef.current.set(sessionId, handlers)

    return () => {
      const current = terminalOutputHandlersRef.current.get(sessionId)
      if (!current) {
        return
      }
      current.delete(handler)
      if (current.size === 0) {
        terminalOutputHandlersRef.current.delete(sessionId)
      }
    }
  }, [])

  const subscribeTerminalExit = useCallback((sessionId: string, handler: TerminalExitHandler) => {
    const handlers = terminalExitHandlersRef.current.get(sessionId) ?? new Set<TerminalExitHandler>()
    handlers.add(handler)
    terminalExitHandlersRef.current.set(sessionId, handlers)

    return () => {
      const current = terminalExitHandlersRef.current.get(sessionId)
      if (!current) {
        return
      }
      current.delete(handler)
      if (current.size === 0) {
        terminalExitHandlersRef.current.delete(sessionId)
      }
    }
  }, [])

  const value = useMemo(
    () => ({
      connected,
      joinWorkingDirectory,
      leaveWorkingDirectory,
      joinTasks,
      leaveTasks,
      joinFile,
      leaveFile,
      joinTerminal,
      leaveTerminal,
      sendTerminalInput,
      resizeTerminal,
      subscribeTerminalOutput,
      subscribeTerminalExit,
    }),
    [
      connected,
      joinFile,
      joinTerminal,
      joinWorkingDirectory,
      leaveFile,
      leaveTerminal,
      leaveWorkingDirectory,
      joinTasks,
      leaveTasks,
      resizeTerminal,
      sendTerminalInput,
      subscribeTerminalExit,
      subscribeTerminalOutput,
    ],
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
