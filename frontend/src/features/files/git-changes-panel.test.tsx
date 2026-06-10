import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as gitApi from '@/api/git'
import type { GitFileStatus } from '@/api/schemas'
import { GitChangesPanel } from './git-changes-panel'

vi.mock('@/api/git')

const changes: GitFileStatus[] = [
  { path: 'src/app.ts', status: 'Modified', originalPath: null },
  { path: 'deleted.txt', status: 'Deleted', originalPath: null },
]

function renderPanel(onSelectChange = vi.fn()) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <GitChangesPanel workingDirectoryId="ws-1" selectedPath="src/app.ts" onSelectChange={onSelectChange} />
    </QueryClientProvider>,
  )

  return { onSelectChange }
}

describe('GitChangesPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(gitApi.getGitStatus).mockResolvedValue(changes)
  })

  afterEach(() => {
    cleanup()
  })

  it('renders changed files including deleted entries', async () => {
    renderPanel()

    expect(await screen.findByText('app.ts')).toBeInTheDocument()
    expect(screen.getByText('deleted.txt')).toBeInTheDocument()
    expect(screen.getByTitle('Modificado')).toHaveTextContent('M')
    expect(screen.getByTitle('Excluido')).toHaveTextContent('D')
  })

  it('emits the selected change payload', async () => {
    const { onSelectChange } = renderPanel()

    await userEvent.click(await screen.findByRole('button', { name: /deleted.txt/i }))

    expect(onSelectChange).toHaveBeenCalledWith(changes[1])
  })

  it('refreshes git status on demand', async () => {
    renderPanel()
    await screen.findByText('app.ts')

    await userEvent.click(screen.getByRole('button', { name: 'Recarregar alteracoes do git' }))

    await waitFor(() => expect(gitApi.getGitStatus).toHaveBeenCalledTimes(2))
  })

  it('shows an empty state', async () => {
    vi.mocked(gitApi.getGitStatus).mockResolvedValue([])

    renderPanel()

    expect(await screen.findByText('Nenhuma alteracao detectada.')).toBeInTheDocument()
  })
})
