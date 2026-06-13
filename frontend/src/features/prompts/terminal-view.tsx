import { FitAddon } from '@xterm/addon-fit'
import { Terminal } from '@xterm/xterm'
import '@xterm/xterm/css/xterm.css'
import { useEffect, useRef } from 'react'
import { base64ToBytes, bytesToBase64 } from '@/lib/base64'
import { usePromptHub } from '@/realtime/prompt-hub'

type TerminalViewProps = {
  sessionId: string
  active: boolean
}

export function TerminalView({ sessionId, active }: TerminalViewProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
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

    const fit = () => {
      fitAddon.fit()
      resizeTerminal(sessionId, term.cols, term.rows)
    }

    const resizeObserver = new ResizeObserver(() => {
      if (active) {
        fit()
      }
    })
    resizeObserver.observe(container)

    joinTerminal(sessionId)

    const unsubscribeOutput = subscribeTerminalOutput(sessionId, (dataBase64) => {
      const bytes = base64ToBytes(dataBase64)
      term.write(bytes)
    })

    const unsubscribeExit = subscribeTerminalExit(sessionId, (exitCode) => {
      term.writeln(`\r\n[Process exited with code ${exitCode}]`)
    })

    const dataDisposable = term.onData((data) => {
      const bytes = new TextEncoder().encode(data)
      sendTerminalInput(sessionId, bytesToBase64(bytes))
    })

    const resizeDisposable = term.onResize(({ cols, rows }) => {
      resizeTerminal(sessionId, cols, rows)
    })

    if (active) {
      requestAnimationFrame(fit)
      term.focus()
    }

    return () => {
      dataDisposable.dispose()
      resizeDisposable.dispose()
      unsubscribeOutput()
      unsubscribeExit()
      leaveTerminal(sessionId)
      resizeObserver.disconnect()
      term.dispose()
    }
  }, [
    active,
    joinTerminal,
    leaveTerminal,
    resizeTerminal,
    sendTerminalInput,
    sessionId,
    subscribeTerminalExit,
    subscribeTerminalOutput,
  ])

  useEffect(() => {
    if (active) {
      requestAnimationFrame(() => {
        const container = containerRef.current
        if (!container) {
          return
        }
        const textarea = container.querySelector('textarea')
        textarea?.focus()
      })
    }
  }, [active])

  return (
    <div
      ref={containerRef}
      className={active ? 'h-[min(70vh,640px)] w-full overflow-hidden rounded-md border border-border bg-[#0f1117] p-1' : 'hidden'}
      aria-hidden={!active}
    />
  )
}