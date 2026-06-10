import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type * as React from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { WorkingDirectory } from '@/api/schemas'
import * as workingDirectoriesApi from '@/api/working-directories'
import { WorkspaceList } from './workspace-list'

vi.mock('@tanstack/react-router', () => ({
  Link: ({
    children,
    className,
  }: {
    children: React.ReactNode
    className?: string
  }) => (
    <a href="/workspaces/ws-1" className={className}>
      {children}
    </a>
  ),
}))

vi.mock('@/api/working-directories')

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

const workspace: WorkingDirectory = {
  id: 'ws-1',
  name: 'repo',
  absolutePath: 'D:/repo',
  respectGitignore: true,
  enableAiContext: false,
  taskNumberPattern: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
}

function renderList() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <WorkspaceList />
    </QueryClientProvider>,
  )
}

describe('WorkspaceList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(workingDirectoriesApi.listWorkingDirectories).mockResolvedValue([workspace])
    vi.mocked(workingDirectoriesApi.deleteWorkingDirectory).mockResolvedValue(undefined)
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  it('deletes a workspace after confirmation', async () => {
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true)
    renderList()

    await screen.findByText('repo')
    await userEvent.click(screen.getByRole('button', { name: 'Excluir workspace repo' }))

    expect(confirm).toHaveBeenCalledWith(expect.stringContaining('A pasta no disco nao sera apagada.'))
    await waitFor(() => expect(workingDirectoriesApi.deleteWorkingDirectory).toHaveBeenCalled())
    expect(vi.mocked(workingDirectoriesApi.deleteWorkingDirectory).mock.calls[0][0]).toBe('ws-1')
  })

  it('does not delete when confirmation is cancelled', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    renderList()

    await screen.findByText('repo')
    await userEvent.click(screen.getByRole('button', { name: 'Excluir workspace repo' }))

    expect(workingDirectoriesApi.deleteWorkingDirectory).not.toHaveBeenCalled()
  })
})
