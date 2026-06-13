import { FitAddon } from '@xterm/addon-fit'
import { Terminal } from '@xterm/xterm'
import '@xterm/xterm/css/xterm.css'
import { useEffect, useRef } from 'react'
import { base64ToBytes, bytesToBase64 } from '@/lib/base64'
import { usePromptHub } from '@/realtime/prompt-hub'

type TerminalViewProps = {
  sessionId: string
  active: boolean
  onSessionExit?: (sessionId: string, exitCode: number) => void
}

export function TerminalView({ sessionId, active, onSessionExit }: TerminalViewProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const terminalRef = useRef<{ term: Terminal; fitAddon: FitAddon } | null>(null)
  const {
    joinTerminal,
    leaveTerminal,
    sendTerminalInput,
    resizeTerminal,
    subscribeTerminalOutput,
    subscribeTerminalExit,
  } = usePromptHub()

  useEffect(() => {
    const container = containerRef.current
    if (!container) {
      return
    }

    const term = new Terminal({
      cursorBlink: true,
      fontFamily: 'var(--font-mono, "JetBrains Mono Variable", monospace)',
      fontSize: 13,
      theme: {
        background: '#0f1117',
        foreground: '#e6edf3',
        cursor: '#e6edf3',
      },
      convertEol: false,
    })
    const fitAddon = new FitAddon()
    term.loadAddon(fitAddon)
    term.open(container)
    terminalRef.current = { term, fitAddon }

    joinTerminal(sessionId)

    const unsubscribeOutput = subscribeTerminalOutput(sessionId, (dataBase64) => {
      const bytes = base64ToBytes(dataBase64)
      term.write(bytes)
    })

    const unsubscribeExit = subscribeTerminalExit(sessionId, (exitCode) => {
      term.writeln(`\r\n[Process exited with code ${exitCode}]`)
      onSessionExit?.(sessionId, exitCode)
    })

    const dataDisposable = term.onData((data) => {
      const bytes = new TextEncoder().encode(data)
      sendTerminalInput(sessionId, bytesToBase64(bytes))
    })

    const resizeDisposable = term.onResize(({ cols, rows }) => {
      resizeTerminal(sessionId, cols, rows)
    })

    return () => {
      dataDisposable.dispose()
      resizeDisposable.dispose()
      unsubscribeOutput()
      unsubscribeExit()
      leaveTerminal(sessionId)
      term.dispose()
      terminalRef.current = null
    }
  }, [
    joinTerminal,
    leaveTerminal,
    onSessionExit,
    resizeTerminal,
    sendTerminalInput,
    sessionId,
    subscribeTerminalExit,
    subscribeTerminalOutput,
  ])

  useEffect(() => {
    const container = containerRef.current
    const terminal = terminalRef.current
    if (!container || !terminal) {
      return
    }

    const fit = () => {
      if (!active) {
        return
      }

      terminal.fitAddon.fit()
      resizeTerminal(sessionId, terminal.term.cols, terminal.term.rows)
    }

    const resizeObserver = new ResizeObserver(() => fit())
    resizeObserver.observe(container)

    if (active) {
      requestAnimationFrame(fit)
      terminal.term.focus()
    }

    return () => resizeObserver.disconnect()
  }, [active, resizeTerminal, sessionId])

  return (
    <div
      ref={containerRef}
      className={active ? 'h-[min(70vh,640px)] w-full overflow-hidden rounded-md border border-border bg-[#0f1117] p-1' : 'hidden'}
      aria-hidden={!active}
    />
  )
}