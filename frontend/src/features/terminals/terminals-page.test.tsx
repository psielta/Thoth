import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { TerminalsPage } from './terminals-page'

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children }: { children?: ReactNode }) => <a>{children}</a>,
}))

vi.mock('@/features/prompts/terminal-view', () => ({
  TerminalView: () => <div data-testid="terminal-view" />,
}))

vi.mock('@/realtime/prompt-hub', () => ({
  usePromptHub: () => ({ connected: true }),
}))

const getTerminalCapabilities = vi.fn()
const listAllTerminals = vi.fn()
const closeTerminal = vi.fn()
const createTerminal = vi.fn()

vi.mock('@/api/terminals', () => ({
  getTerminalCapabilities: (...args: unknown[]) => getTerminalCapabilities(...args),
  listAllTerminals: (...args: unknown[]) => listAllTerminals(...args),
  closeTerminal: (...args: unknown[]) => closeTerminal(...args),
  createTerminal: (...args: unknown[]) => createTerminal(...args),
}))

const sampleGroup = {
  promptId: '11111111-1111-4111-8111-111111111111',
  promptTitle: 'Refatorar auth',
  workingDirectoryId: '22222222-2222-4222-8222-222222222222',
  workingDirectoryName: 'repo',
  isArchived: false,
  terminals: [
    {
      id: '33333333-3333-4333-8333-333333333333',
      promptId: '11111111-1111-4111-8111-111111111111',
      shell: 'pwsh.exe',
      cwd: 'D:/repo',
      createdAtUtc: '2026-06-13T12:00:00Z',
    },
  ],
}

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(
    <QueryClientProvider client={client}>
      <TerminalsPage />
    </QueryClientProvider>,
  )
}

describe('TerminalsPage', () => {
  afterEach(() => {
    cleanup()
    vi.clearAllMocks()
  })

  it('shows the disabled state and does not fetch groups', async () => {
    getTerminalCapabilities.mockResolvedValue({ enabled: false })
    renderPage()

    expect(await screen.findByText(/desativados nesta inst/i)).toBeInTheDocument()
    expect(listAllTerminals).not.toHaveBeenCalled()
  })

  it('shows the empty state when nothing is running', async () => {
    getTerminalCapabilities.mockResolvedValue({ enabled: true })
    listAllTerminals.mockResolvedValue([])
    renderPage()

    expect(await screen.findByText(/Nenhum terminal em execução/i)).toBeInTheDocument()
  })

  it('groups running terminals by prompt with a total count', async () => {
    getTerminalCapabilities.mockResolvedValue({ enabled: true })
    listAllTerminals.mockResolvedValue([sampleGroup])
    renderPage()

    expect(await screen.findByText('Refatorar auth')).toBeInTheDocument()
    expect(screen.getByText('Terminal 1')).toBeInTheDocument()
    expect(screen.getByText(/1 terminal em 1 prompt/i)).toBeInTheDocument()
  })

  it('closes a terminal from a card', async () => {
    getTerminalCapabilities.mockResolvedValue({ enabled: true })
    listAllTerminals.mockResolvedValue([sampleGroup])
    closeTerminal.mockResolvedValue(undefined)

    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Refatorar auth')
    await user.click(screen.getByRole('button', { name: /fechar terminal 1/i }))

    await waitFor(() => {
      expect(closeTerminal).toHaveBeenCalledWith('33333333-3333-4333-8333-333333333333')
    })
  })
})
