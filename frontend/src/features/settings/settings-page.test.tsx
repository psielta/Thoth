import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getAppSettings, updateAppSettings } from '@/api/app-settings'
import { getWorkflowTemplate, updateWorkflowTemplate } from '@/api/workflow'
import { SettingsPage } from './settings-page'

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children }: { children: ReactNode }) => <a href="/">{children}</a>,
}))

vi.mock('@/api/app-settings', () => ({
  getAppSettings: vi.fn(),
  updateAppSettings: vi.fn(),
}))

vi.mock('@/api/workflow', () => ({
  getWorkflowTemplate: vi.fn(),
  updateWorkflowTemplate: vi.fn(),
}))

vi.mock('@/features/workflow/phase-editor', () => ({
  PhaseEditor: () => <div>Editor de fases</div>,
}))

vi.mock('sonner', () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}))

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <SettingsPage />
    </QueryClientProvider>,
  )

  return queryClient
}

describe('SettingsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getAppSettings).mockResolvedValue({
      showAgentTerminalOfferAfterChildPrompt: true,
    })
    vi.mocked(updateAppSettings).mockResolvedValue({
      showAgentTerminalOfferAfterChildPrompt: false,
    })
    vi.mocked(getWorkflowTemplate).mockResolvedValue({
      id: '019e9f6a-a269-7991-95d5-4e602dcf773d',
      name: 'Padrao',
      phases: [],
    })
    vi.mocked(updateWorkflowTemplate).mockResolvedValue({
      id: '019e9f6a-a269-7991-95d5-4e602dcf773d',
      name: 'Padrao',
      phases: [],
    })
  })

  afterEach(() => {
    cleanup()
  })

  it('updates the global child prompt agent offer setting', async () => {
    const user = userEvent.setup()
    renderPage()

    const toggle = await screen.findByLabelText('Ativo')
    await user.click(toggle)

    await waitFor(() => {
      expect(updateAppSettings).toHaveBeenCalled()
    })
    expect(vi.mocked(updateAppSettings).mock.calls[0][0]).toEqual({
      showAgentTerminalOfferAfterChildPrompt: false,
    })
  })
})
