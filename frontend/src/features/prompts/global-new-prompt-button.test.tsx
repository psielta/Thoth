import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as workingDirectoriesApi from '@/api/working-directories'
import type { WorkingDirectory } from '@/api/schemas'
import { GlobalNewPromptButton } from './global-new-prompt-button'

vi.mock('@tanstack/react-router', () => ({
  useParams: () => ({ workspaceId: 'ws-7' }),
}))

vi.mock('@/api/working-directories')

vi.mock('@/features/workflow/new-prompt-drawer', () => ({
  NewPromptDrawer: ({
    defaultWorkingDirectoryId,
    workspaces,
    onClose,
  }: {
    defaultWorkingDirectoryId?: string
    workspaces: WorkingDirectory[]
    onClose: () => void
  }) => (
    <div role="dialog" aria-label="Drawer de novo prompt">
      <span data-testid="default-workspace">{defaultWorkingDirectoryId}</span>
      <span data-testid="workspace-count">{workspaces.length}</span>
      <button type="button" onClick={onClose}>
        Fechar drawer
      </button>
    </div>
  ),
}))

const sampleWorkspace: WorkingDirectory = {
  id: 'ws-7',
  name: 'repo',
  absolutePath: 'D:/repo',
  respectGitignore: true,
  enableAiContext: false,
  taskNumberPattern: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
}

function renderButton() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <GlobalNewPromptButton />
    </QueryClientProvider>,
  )
}

describe('GlobalNewPromptButton', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(workingDirectoriesApi.listWorkingDirectories).mockResolvedValue([sampleWorkspace])
  })

  afterEach(() => {
    cleanup()
  })

  it('does not fetch workspaces until the button is clicked', () => {
    renderButton()

    expect(screen.getByRole('button', { name: 'Novo prompt' })).toBeInTheDocument()
    expect(workingDirectoriesApi.listWorkingDirectories).not.toHaveBeenCalled()
  })

  it('opens the new-prompt drawer preserving the current workspace context', async () => {
    renderButton()

    await userEvent.click(screen.getByRole('button', { name: 'Novo prompt' }))

    expect(await screen.findByRole('dialog', { name: 'Drawer de novo prompt' })).toBeInTheDocument()
    expect(screen.getByTestId('default-workspace')).toHaveTextContent('ws-7')
    expect(screen.getByTestId('workspace-count')).toHaveTextContent('1')
    expect(screen.queryByRole('button', { name: 'Novo prompt' })).not.toBeInTheDocument()
  })

  it('shows the floating button again after closing the drawer', async () => {
    renderButton()

    await userEvent.click(screen.getByRole('button', { name: 'Novo prompt' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Fechar drawer' }))

    expect(screen.getByRole('button', { name: 'Novo prompt' })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
