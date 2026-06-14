import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { TerminalFrame } from './terminal-frame'

describe('TerminalFrame', () => {
  afterEach(cleanup)

  it('keeps terminal sizing and overflow isolated for the prompt layout', () => {
    render(
      <TerminalFrame variant="prompt">
        <div>Terminal</div>
      </TerminalFrame>,
    )

    const frame = screen.getByText('Terminal').parentElement
    expect(frame).toHaveClass('relative')
    expect(frame).toHaveClass('min-h-0')
    expect(frame).toHaveClass('overflow-hidden')
    expect(frame).toHaveClass('h-[min(70vh,640px)]')
  })

  it('uses the drawer variant with full available height', () => {
    render(
      <TerminalFrame variant="drawer">
        <div>Terminal</div>
      </TerminalFrame>,
    )

    const frame = screen.getByText('Terminal').parentElement
    expect(frame).toHaveClass('h-full')
    expect(frame).toHaveClass('min-h-0')
  })
})
