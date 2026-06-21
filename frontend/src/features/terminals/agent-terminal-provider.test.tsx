import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getAppSettings } from '@/api/app-settings'
import { queryKeys } from '@/api/query-keys'
import type { Prompt } from '@/api/schemas'
import { getTerminalCapabilities } from '@/api/terminals'
import { AgentTerminalProvider } from './agent-terminal-provider'
import { useAgentTerminal } from './use-agent-terminal'

vi.mock('@/api/app-settings', () => ({
  getAppSettings: vi.fn(),
}))

vi.mock('@/api/terminals', () => ({
  getTerminalCapabilities: vi.fn(),
  createTerminal: vi.fn(),
}))

const prompt: Prompt = {
  id: '019e9f6a-a5c7-78b8-9683-69966d7ecdbc',
  workingDirectoryId: '019e9f6a-9fb2-7f24-ac3a-bf099d2c93c0',
  parentPromptId: '019e9f6a-a269-7991-95d5-4e602dcf773d',
  futureTaskId: null,
  taskNumber: null,
  title: 'Revisar PR #42',
  content: '/review a PR',
  targetAgent: 'Codex',
  kind: 'General',
  status: 'Draft',
  currentVersion: 1,
  rowVersion: '0',
  createdAtUtc: '2026-05-31T00:00:00Z',
  updatedAtUtc: '2026-05-31T00:00:00Z',
  mentions: [],
}

function RequestAgentButton() {
  const { requestAgentTerminal } = useAgentTerminal()
  return (
    <button type="button" onClick={() => requestAgentTerminal(prompt)}>
      solicitar
    </button>
  )
}

function renderProvider() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <AgentTerminalProvider>
        <RequestAgentButton />
      </AgentTerminalProvider>
    </QueryClientProvider>,
  )

  return queryClient
}

describe('AgentTerminalProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getTerminalCapabilities).mockResolvedValue({ enabled: true })
  })

  afterEach(() => {
    cleanup()
  })

  it('opens the agent terminal offer when the global setting is enabled', async () => {
    vi.mocked(getAppSettings).mockResolvedValue({
      showAgentTerminalOfferAfterChildPrompt: true,
    })
    const user = userEvent.setup()
    const queryClient = renderProvider()

    await waitFor(() => {
      expect(queryClient.getQueryData(queryKeys.appSettings.current())).toEqual({
        showAgentTerminalOfferAfterChildPrompt: true,
      })
    })
    await user.click(screen.getByRole('button', { name: 'solicitar' }))

    expect(screen.getByText('Criar terminal com agente?')).toBeInTheDocument()
  })

  it('does not open the agent terminal offer when the global setting is disabled', async () => {
    vi.mocked(getAppSettings).mockResolvedValue({
      showAgentTerminalOfferAfterChildPrompt: false,
    })
    const user = userEvent.setup()
    const queryClient = renderProvider()

    await waitFor(() => {
      expect(queryClient.getQueryData(queryKeys.appSettings.current())).toEqual({
        showAgentTerminalOfferAfterChildPrompt: false,
      })
    })
    await user.click(screen.getByRole('button', { name: 'solicitar' }))

    expect(screen.queryByText('Criar terminal com agente?')).not.toBeInTheDocument()
  })
})
