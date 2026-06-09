import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as filesApi from '@/api/files'
import type { FileTreeNode } from '@/api/schemas'
import { WorkspaceFileTree } from './workspace-file-tree'

vi.mock('@/api/files')

const sampleNodes: FileTreeNode[] = [
  { name: 'src', relativePath: 'src', isDirectory: true },
  { name: 'README.md', relativePath: 'README.md', isDirectory: false },
]

function renderTree(onSelectFile = vi.fn()) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <WorkspaceFileTree workingDirectoryId="ws-1" onSelectFile={onSelectFile} />
    </QueryClientProvider>,
  )

  return { onSelectFile }
}

describe('WorkspaceFileTree', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(filesApi.browseDirectory).mockResolvedValue(sampleNodes)
    vi.mocked(filesApi.searchFiles).mockResolvedValue([])
  })

  afterEach(() => {
    cleanup()
  })

  it('renders the root nodes returned by the API', async () => {
    renderTree()

    expect(await screen.findByText('README.md')).toBeInTheDocument()
    expect(filesApi.browseDirectory).toHaveBeenCalledWith('ws-1', '')
  })

  it('reloads the tree when the refresh button is clicked', async () => {
    renderTree()
    await screen.findByText('README.md')
    expect(filesApi.browseDirectory).toHaveBeenCalledTimes(1)

    await userEvent.click(screen.getByRole('button', { name: 'Recarregar arquivos do workspace' }))

    await waitFor(() => expect(filesApi.browseDirectory).toHaveBeenCalledTimes(2))
    expect(filesApi.browseDirectory).toHaveBeenLastCalledWith('ws-1', '')
    expect(await screen.findByText('README.md')).toBeInTheDocument()
  })
})
