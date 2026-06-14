import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup } from '@testing-library/react'
import { TerminalsPanel } from './terminals-panel'

vi.mock('./terminal-view', () => ({
  TerminalView: () => <div data-testid="terminal-view" />,
}))

const createTerminal = vi.fn()
const closeTerminal = vi.fn()
const listTerminals = vi.fn()

vi.mock('@/api/terminals', () => ({
  createTerminal: (...args: unknown[]) => createTerminal(...args),
  closeTerminal: (...args: unknown[]) => closeTerminal(...args),
  listTerminals: (...args: unknown[]) => listTerminals(...args),
}))

const hubMocks = {
  joinTerminal: vi.fn(),
  leaveTerminal: vi.fn(),
  sendTerminalInput: vi.fn(),
  resizeTerminal: vi.fn(),
  subscribeTerminalOutput: vi.fn(() => () => undefined),
  subscribeTerminalExit: vi.fn(() => () => undefined),
}

vi.mock('@/realtime/prompt-hub', () => ({
  usePromptHub: () => hubMocks,
}))

function renderPanel(promptId = '11111111-1111-4111-8111-111111111111') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <TerminalsPanel promptId={promptId} />
    </QueryClientProvider>,
  )
}

describe('TerminalsPanel', () => {
  afterEach(() => {
    cleanup()
    vi.clearAllMocks()
  })

  it('creates a terminal when clicking Novo terminal', async () => {
    listTerminals.mockResolvedValue([])
    createTerminal.mockResolvedValue({
      id: '22222222-2222-4222-8222-222222222222',
      promptId: '11111111-1111-4111-8111-111111111111',
      shell: 'pwsh.exe',
      cwd: 'C:/repo',
      createdAtUtc: '2026-06-13T12:00:00Z',
    })

    const user = userEvent.setup()
    renderPanel()

    await user.click(screen.getByRole('button', { name: /^Novo terminal$/i }))

    await waitFor(() => {
      expect(createTerminal).toHaveBeenCalledWith('11111111-1111-4111-8111-111111111111', {})
    })
    expect(await screen.findByRole('button', { name: /^Terminal 1$/i })).toBeInTheDocument()
  })

  it('creates a terminal with Claude plan launch command', async () => {
    listTerminals.mockResolvedValue([])
    createTerminal.mockResolvedValue({
      id: '44444444-4444-4444-8444-444444444444',
      promptId: '11111111-1111-4111-8111-111111111111',
      shell: 'powershell.exe',
      cwd: 'C:/repo',
      createdAtUtc: '2026-06-13T12:00:00Z',
    })

    const user = userEvent.setup()
    renderPanel()

    await user.click(screen.getByRole('button', { name: /abrir terminal com agente/i }))
    await user.click(screen.getByRole('menuitem', { name: /planejar no claude/i }))

    await waitFor(() => {
      expect(createTerminal).toHaveBeenCalledWith('11111111-1111-4111-8111-111111111111', {
        agentLaunch: 'ClaudePlan',
      })
    })
    expect(await screen.findByRole('button', { name: /^Claude Plan$/i })).toBeInTheDocument()
  })

  it('creates a terminal with the selected agent launch command', async () => {
    listTerminals.mockResolvedValue([])
    createTerminal.mockResolvedValue({
      id: '33333333-3333-4333-8333-333333333333',
      promptId: '11111111-1111-4111-8111-111111111111',
      shell: 'powershell.exe',
      cwd: 'C:/repo',
      createdAtUtc: '2026-06-13T12:00:00Z',
    })

    const user = userEvent.setup()
    renderPanel()

    await user.click(screen.getByRole('button', { name: /abrir terminal com agente/i }))
    await user.click(screen.getByRole('menuitem', { name: /codex/i }))

    await waitFor(() => {
      expect(createTerminal).toHaveBeenCalledWith('11111111-1111-4111-8111-111111111111', { agentLaunch: 'Codex' })
    })
    expect(await screen.findByRole('button', { name: /^Codex$/i })).toBeInTheDocument()
  })

  it('lists restored sessions and closes one', async () => {
    listTerminals.mockResolvedValue([
      {
        id: '22222222-2222-4222-8222-222222222222',
        promptId: '11111111-1111-4111-8111-111111111111',
        shell: 'pwsh.exe',
        cwd: 'C:/repo',
        createdAtUtc: '2026-06-13T12:00:00Z',
      },
    ])
    closeTerminal.mockResolvedValue(undefined)

    const user = userEvent.setup()
    renderPanel()

    expect(await screen.findByRole('button', { name: /^Terminal 1$/i })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /fechar terminal 1/i }))

    await waitFor(() => {
      expect(closeTerminal).toHaveBeenCalledWith('22222222-2222-4222-8222-222222222222')
    })
  })

  it('shows child-prompt terminals with a Filho badge and closes one', async () => {
    listTerminals.mockResolvedValue([
      {
        id: '22222222-2222-4222-8222-222222222222',
        promptId: '11111111-1111-4111-8111-111111111111',
        shell: 'pwsh.exe',
        cwd: 'C:/repo',
        createdAtUtc: '2026-06-13T12:00:00Z',
      },
      {
        id: '66666666-6666-4666-8666-666666666666',
        promptId: '44444444-4444-4444-8444-444444444444',
        shell: 'pwsh.exe',
        cwd: 'C:/repo',
        createdAtUtc: '2026-06-13T13:00:00Z',
        isChild: true,
        ownerPromptTitle: 'Revisar PR #42',
      },
    ])
    closeTerminal.mockResolvedValue(undefined)

    const user = userEvent.setup()
    renderPanel()

    // A aba do filho exibe o titulo do filho e o badge "Filho"; a propria continua "Terminal 1".
    expect(await screen.findByText('Revisar PR #42')).toBeInTheDocument()
    expect(screen.getByText('Filho')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^Terminal 1$/i })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /fechar Revisar PR #42/i }))

    await waitFor(() => {
      expect(closeTerminal).toHaveBeenCalledWith('66666666-6666-4666-8666-666666666666')
    })
  })
})