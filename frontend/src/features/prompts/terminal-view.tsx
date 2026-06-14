import { FitAddon } from '@xterm/addon-fit'
import { Terminal } from '@xterm/xterm'
import '@xterm/xterm/css/xterm.css'
import './terminal-view.css'
import { ArrowDownToLine, ArrowUpToLine } from 'lucide-react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { getTerminalOutputHistory } from '@/api/terminals'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { base64ToBytes, bytesToBase64 } from '@/lib/base64'
import { cn } from '@/lib/utils'
import { usePromptHub } from '@/realtime/prompt-hub'
import {
  applyTerminalOutputChunk,
  applyTerminalOutputHistory,
  type TerminalOutputChunk,
} from './terminal-output-buffer'

const TERMINAL_FONT_FAMILY =
  '"Cascadia Code", "Cascadia Mono", Consolas, "JetBrains Mono", ui-monospace, monospace'

type TerminalViewProps = {
  sessionId: string
  active: boolean
  fontSize: number
  onZoom?: (delta: number) => void
  onSessionExit?: (sessionId: string, exitCode: number) => void
  onKeyboardShortcut?: (event: KeyboardEvent) => boolean
}

type BadgeVariant = 'neutral' | 'green' | 'amber' | 'blue' | 'red'

function scheduleTerminalFit(
  fitAddon: FitAddon,
  term: Terminal,
  options?: { notifyBackend?: boolean; onSized?: (cols: number, rows: number) => void },
) {
  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      try {
        fitAddon.fit()
      } catch {
        return
      }

      if (options?.notifyBackend) {
        options.onSized?.(term.cols, term.rows)
      }

      term.refresh(0, term.rows - 1)
    })
  })
}

export function TerminalView({
  sessionId,
  active,
  fontSize,
  onZoom,
  onSessionExit,
  onKeyboardShortcut,
}: TerminalViewProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const terminalRef = useRef<{ term: Terminal; fitAddon: FitAddon } | null>(null)
  const isAtBottomRef = useRef(true)
  const onZoomRef = useRef(onZoom)
  const onKeyboardShortcutRef = useRef(onKeyboardShortcut)
  const activeRef = useRef(active)
  const [hasUnreadOutput, setHasUnreadOutput] = useState(false)
  const [historyTruncated, setHistoryTruncated] = useState(false)
  const [historyUnavailable, setHistoryUnavailable] = useState(false)
  const [gapDetected, setGapDetected] = useState(false)
  const [exited, setExited] = useState(false)
  const {
    connected,
    joinTerminal,
    leaveTerminal,
    sendTerminalInput,
    resizeTerminal,
    subscribeTerminalOutput,
    subscribeTerminalExit,
  } = usePromptHub()

  const updateBottomState = useCallback((term: Terminal) => {
    const buffer = term.buffer.active
    const isAtBottom = buffer.baseY - buffer.viewportY <= 1
    isAtBottomRef.current = isAtBottom
    if (isAtBottom) {
      setHasUnreadOutput(false)
    }

    return isAtBottom
  }, [])

  const scrollToTop = useCallback(() => {
    const terminal = terminalRef.current
    if (!terminal) {
      return
    }

    terminal.term.scrollToTop()
    updateBottomState(terminal.term)
  }, [updateBottomState])

  const scrollToBottom = useCallback(() => {
    const terminal = terminalRef.current
    if (!terminal) {
      return
    }

    terminal.term.scrollToBottom()
    setHasUnreadOutput(false)
    updateBottomState(terminal.term)
  }, [updateBottomState])

  useEffect(() => {
    onZoomRef.current = onZoom
    onKeyboardShortcutRef.current = onKeyboardShortcut
    activeRef.current = active
  }, [active, onKeyboardShortcut, onZoom])

  useEffect(() => {
    const container = containerRef.current
    if (!container) {
      return
    }

    const term = new Terminal({
      cursorBlink: true,
      convertEol: false,
      scrollback: 10_000,
      smoothScrollDuration: 0,
      fastScrollModifier: 'alt',
      fontFamily: TERMINAL_FONT_FAMILY,
      fontSize,
      lineHeight: 1,
      theme: {
        background: '#0f1117',
        foreground: '#e6edf3',
        cursor: '#e6edf3',
        selectionBackground: '#264f78',
        black: '#484f58',
        red: '#ff7b72',
        green: '#3fb950',
        yellow: '#d29922',
        blue: '#58a6ff',
        magenta: '#bc8cff',
        cyan: '#39c5cf',
        white: '#b1bac4',
        brightBlack: '#6e7681',
        brightRed: '#ffa198',
        brightGreen: '#56d364',
        brightYellow: '#e3b341',
        brightBlue: '#79c0ff',
        brightMagenta: '#d2a8ff',
        brightCyan: '#56d4dd',
        brightWhite: '#ffffff',
      },
    })
    const fitAddon = new FitAddon()
    term.loadAddon(fitAddon)
    term.open(container)
    terminalRef.current = { term, fitAddon }
    let disposed = false
    let outputEndOffset = 0
    let historyLoaded = false
    let resyncing = false
    let historyUnavailableLocal = false
    let lastGapResyncKey: string | null = null
    const pendingOutput: TerminalOutputChunk[] = []

    const writeBytes = (bytes: Uint8Array, options?: { forceFollow?: boolean }) => {
      if (bytes.length === 0) {
        return
      }

      const viewportBefore = term.buffer.active.viewportY
      const shouldFollow = options?.forceFollow ?? isAtBottomRef.current
      term.write(bytes, () => {
        if (disposed) {
          return
        }

        if (shouldFollow) {
          term.scrollToBottom()
        } else {
          term.scrollToLine(viewportBefore)
          setHasUnreadOutput(true)
        }

        updateBottomState(term)
      })
    }

    const forceWriteAfterGap = (chunk: TerminalOutputChunk) => {
      const bytes = base64ToBytes(chunk.dataBase64)
      setGapDetected(true)
      term.writeln('\r\n[Historico anterior indisponivel; retomando saida ao vivo.]')
      outputEndOffset = chunk.startOffset
      writeBytes(bytes)
      outputEndOffset = chunk.startOffset + bytes.length
    }

    const applyChunk = (chunk: TerminalOutputChunk) => {
      const result = applyTerminalOutputChunk({ endOffset: outputEndOffset }, chunk)
      if (result.type === 'ignored') {
        return true
      }

      if (result.type === 'gap') {
        return false
      }

      lastGapResyncKey = null
      writeBytes(result.bytes)
      outputEndOffset = result.endOffset
      return true
    }

    const flushPendingOutput = () => {
      if (!historyLoaded || resyncing) {
        return
      }

      pendingOutput.sort((left, right) => left.startOffset - right.startOffset)
      while (pendingOutput.length > 0) {
        const chunk = pendingOutput.shift()
        if (!chunk) {
          return
        }

        const result = applyTerminalOutputChunk({ endOffset: outputEndOffset }, chunk)
        if (result.type === 'gap') {
          const gapKey = `${result.expectedOffset}:${result.startOffset}`
          if (historyUnavailableLocal || lastGapResyncKey === gapKey) {
            lastGapResyncKey = null
            forceWriteAfterGap(chunk)
            continue
          }

          lastGapResyncKey = gapKey
          pendingOutput.unshift(chunk)
          void resyncFromHistory()
          return
        }

        if (result.type === 'write') {
          lastGapResyncKey = null
          writeBytes(result.bytes)
          outputEndOffset = result.endOffset
        }
      }
    }

    const resyncFromHistory = async () => {
      if (resyncing) {
        return
      }

      resyncing = true
      try {
        const history = await getTerminalOutputHistory(sessionId)
        if (disposed) {
          return
        }

        historyUnavailableLocal = false
        historyLoaded = true
        setHistoryUnavailable(false)
        setHistoryTruncated(history.isTruncated)

        const result = applyTerminalOutputHistory({ endOffset: outputEndOffset }, history)
        if (result.type === 'reset') {
          term.clear()
          setGapDetected(true)
          setHistoryTruncated(result.isTruncated)
          writeBytes(result.bytes, { forceFollow: true })
          outputEndOffset = result.endOffset
          return
        }

        if (result.type === 'write') {
          setHistoryTruncated(result.isTruncated)
          writeBytes(result.bytes, { forceFollow: outputEndOffset === 0 })
          outputEndOffset = result.endOffset
          return
        }

        setHistoryTruncated(result.isTruncated)
      } catch {
        if (!disposed) {
          historyLoaded = true
          historyUnavailableLocal = true
          setHistoryUnavailable(true)
        }
      } finally {
        resyncing = false
        if (!disposed) {
          flushPendingOutput()
        }
      }
    }

    const notifyBackendResize = (cols: number, rows: number) => {
      if (!activeRef.current) {
        return
      }

      resizeTerminal(sessionId, cols, rows)
    }

    if (activeRef.current) {
      scheduleTerminalFit(fitAddon, term, { notifyBackend: true, onSized: notifyBackendResize })
    }

    const scrollDisposable = term.onScroll(() => updateBottomState(term))

    const unsubscribeOutput = subscribeTerminalOutput(sessionId, (startOffset, dataBase64) => {
      const chunk = { startOffset, dataBase64 }
      if (!historyLoaded || resyncing) {
        pendingOutput.push(chunk)
        return
      }

      if (!applyChunk(chunk)) {
        pendingOutput.push(chunk)
        void resyncFromHistory()
      }
    })

    joinTerminal(sessionId)
    void resyncFromHistory()

    const unsubscribeExit = subscribeTerminalExit(sessionId, (exitCode) => {
      setExited(true)
      term.writeln(`\r\n[Process exited with code ${exitCode}]`)
      onSessionExit?.(sessionId, exitCode)
    })

    const dataDisposable = term.onData((data) => {
      const bytes = new TextEncoder().encode(data)
      sendTerminalInput(sessionId, bytesToBase64(bytes))
    })

    term.attachCustomKeyEventHandler((event) => {
      if (!activeRef.current || event.type !== 'keydown') {
        return true
      }

      const handler = onKeyboardShortcutRef.current
      if (!handler) {
        return true
      }

      return !handler(event)
    })

    const onWheel = (event: WheelEvent) => {
      if (event.ctrlKey || event.metaKey) {
        if (!activeRef.current || !onZoomRef.current) {
          return
        }

        event.preventDefault()
        event.stopPropagation()
        onZoomRef.current(event.deltaY < 0 ? 1 : -1)
        return
      }

      event.stopPropagation()
    }
    container.addEventListener('wheel', onWheel, { passive: false })

    return () => {
      disposed = true
      container.removeEventListener('wheel', onWheel)
      scrollDisposable.dispose()
      dataDisposable.dispose()
      unsubscribeOutput()
      unsubscribeExit()
      leaveTerminal(sessionId)
      term.dispose()
      terminalRef.current = null
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fontSize handled in dedicated effect
  }, [
    joinTerminal,
    leaveTerminal,
    onSessionExit,
    resizeTerminal,
    sendTerminalInput,
    sessionId,
    subscribeTerminalExit,
    subscribeTerminalOutput,
    updateBottomState,
  ])

  useEffect(() => {
    const terminal = terminalRef.current
    if (!terminal) {
      return
    }

    terminal.term.options.fontSize = fontSize
    scheduleTerminalFit(terminal.fitAddon, terminal.term, {
      notifyBackend: active,
      onSized: (cols, rows) => resizeTerminal(sessionId, cols, rows),
    })
  }, [active, fontSize, resizeTerminal, sessionId])

  useEffect(() => {
    const container = containerRef.current
    const terminal = terminalRef.current
    if (!container || !terminal) {
      return
    }

    if (!active) {
      return
    }

    const fit = () => {
      scheduleTerminalFit(terminal.fitAddon, terminal.term, {
        notifyBackend: true,
        onSized: (cols, rows) => resizeTerminal(sessionId, cols, rows),
      })
      terminal.term.focus()
    }

    const resizeObserver = new ResizeObserver(() => fit())
    resizeObserver.observe(container)
    fit()

    return () => resizeObserver.disconnect()
  }, [active, resizeTerminal, sessionId])

  const connectionBadge = useMemo<{ label: string; variant: BadgeVariant; title: string }>(() => {
    if (exited) {
      return {
        label: 'Sessao encerrada',
        variant: 'neutral',
        title: 'O processo deste terminal foi encerrado.',
      }
    }

    if (historyUnavailable) {
      return {
        label: 'Historico indisponivel',
        variant: 'red',
        title: 'O historico inicial nao foi encontrado para esta sessao.',
      }
    }

    if (!connected) {
      return {
        label: 'Reconectando',
        variant: 'amber',
        title: 'A conexao em tempo real caiu; o terminal sera resincronizado pelo historico.',
      }
    }

    return {
      label: 'Conectado',
      variant: 'green',
      title: 'Recebendo saida do terminal em tempo real.',
    }
  }, [connected, exited, historyUnavailable])

  return (
    <div
      className={cn(
        'absolute inset-0 overflow-hidden',
        active ? 'z-10' : 'z-0 pointer-events-none invisible',
      )}
      aria-hidden={!active}
    >
      <div ref={containerRef} className="thoth-terminal absolute inset-0 overflow-hidden" />

      {active ? (
        <div className="absolute right-2 top-2 z-20 flex max-w-[calc(100%-1rem)] flex-wrap justify-end gap-1.5">
          <Badge
            variant={connectionBadge.variant}
            title={connectionBadge.title}
            className="border border-border/70 bg-card/95 shadow-sm backdrop-blur"
          >
            {connectionBadge.label}
          </Badge>
          {historyTruncated ? (
            <Badge
              variant="amber"
              title="O backend manteve apenas os bytes mais recentes do historico desta sessao."
              className="border border-border/70 bg-card/95 shadow-sm backdrop-blur"
            >
              Historico truncado
            </Badge>
          ) : null}
          {gapDetected ? (
            <Badge
              variant="red"
              title="Uma lacuna de offset foi detectada e a tela foi resincronizada pelo historico disponivel."
              className="border border-border/70 bg-card/95 shadow-sm backdrop-blur"
            >
              Lacuna resincronizada
            </Badge>
          ) : null}
        </div>
      ) : null}

      {active ? (
        <div className="absolute bottom-2 left-2 z-20 flex gap-1.5">
          <Button
            type="button"
            size="icon"
            variant="secondary"
            className="h-7 w-7 bg-card/95 shadow-sm backdrop-blur"
            title="Ir ao topo"
            aria-label="Ir ao topo do terminal"
            onClick={scrollToTop}
          >
            <ArrowUpToLine className="h-3.5 w-3.5" />
          </Button>
          <Button
            type="button"
            size="icon"
            variant="secondary"
            className="h-7 w-7 bg-card/95 shadow-sm backdrop-blur"
            title="Ir ao fim"
            aria-label="Ir ao fim do terminal"
            onClick={scrollToBottom}
          >
            <ArrowDownToLine className="h-3.5 w-3.5" />
          </Button>
        </div>
      ) : null}

      {active && hasUnreadOutput ? (
        <Button
          type="button"
          size="sm"
          variant="secondary"
          className="absolute bottom-2 right-2 z-20 h-7 bg-card/95 px-2 text-[0.7rem] shadow-sm backdrop-blur"
          onClick={scrollToBottom}
        >
          <ArrowDownToLine className="h-3.5 w-3.5" />
          Novas saidas
        </Button>
      ) : null}
    </div>
  )
}
