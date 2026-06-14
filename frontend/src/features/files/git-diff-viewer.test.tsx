import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as filesApi from '@/api/files'
import * as gitApi from '@/api/git'
import { GitDiffViewer } from './git-diff-viewer'

vi.mock('@/api/files')
vi.mock('@/api/git')
vi.mock('./use-file-subscription', () => ({
  useFileSubscription: () => {},
}))
vi.mock('@/components/theme/theme-provider', () => ({
  useTheme: () => ({ resolvedTheme: 'light' }),
}))
vi.mock('./monaco-setup', () => ({
  resolveMonacoTheme: () => 'vs',
}))

type DiffEditorMockProps = {
  original: string
  modified: string
  language: string
}

vi.mock('@monaco-editor/react', () => ({
  DiffEditor: ({ original, modified, language }: DiffEditorMockProps) => (
    <div data-testid="diff-editor" data-original={original} data-modified={modified} data-language={language} />
  ),
}))

function renderViewer(props: Partial<React.ComponentProps<typeof GitDiffViewer>> = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <GitDiffViewer workingDirectoryId="ws-1" path="src/app.ts" status="Modified" {...props} />
    </QueryClientProvider>,
  )
}

describe('GitDiffViewer', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(gitApi.getGitOriginalFile).mockResolvedValue({ content: 'original' })
    vi.mocked(filesApi.getFileContent).mockImplementation(async (_workingDirectoryId, relativePath) => ({
      relativePath,
      content: 'current',
      sizeBytes: 10,
      truncated: false,
      isBinary: false,
    }))
  })

  afterEach(() => {
    cleanup()
  })

  it('fetches original and current content for modified files', async () => {
    renderViewer()

    const editor = await screen.findByTestId('diff-editor')
    expect(editor).toHaveAttribute('data-original', 'original')
    expect(editor).toHaveAttribute('data-modified', 'current')
    expect(editor).toHaveAttribute('data-language', 'typescript')
    expect(gitApi.getGitOriginalFile).toHaveBeenCalledWith('ws-1', 'src/app.ts')
    expect(filesApi.getFileContent).toHaveBeenCalledWith('ws-1', 'src/app.ts')
  })

  it('skips the original side for untracked files', async () => {
    renderViewer({ path: 'untracked.ts', status: 'Untracked' })

    const editor = await screen.findByTestId('diff-editor')
    expect(editor).toHaveAttribute('data-original', '')
    expect(editor).toHaveAttribute('data-modified', 'current')
    expect(gitApi.getGitOriginalFile).not.toHaveBeenCalled()
  })

  it('skips the current side for deleted files', async () => {
    renderViewer({ path: 'deleted.ts', status: 'Deleted' })

    const editor = await screen.findByTestId('diff-editor')
    expect(editor).toHaveAttribute('data-original', 'original')
    expect(editor).toHaveAttribute('data-modified', '')
    expect(filesApi.getFileContent).not.toHaveBeenCalled()
  })

  it('fetches the original path for renamed files', async () => {
    renderViewer({ path: 'src/new.ts', originalPath: 'src/old.ts', status: 'Renamed' })

    await screen.findByTestId('diff-editor')
    expect(gitApi.getGitOriginalFile).toHaveBeenCalledWith('ws-1', 'src/old.ts')
  })

  it('warns when the current file content is truncated', async () => {
    vi.mocked(filesApi.getFileContent).mockResolvedValueOnce({
      relativePath: 'src/app.ts',
      content: 'current',
      sizeBytes: 1_500_000,
      truncated: true,
      isBinary: false,
    })

    renderViewer()

    expect(
      await screen.findByText('Arquivo truncado para visualizacao. Abra no editor local para ver o conteudo completo.'),
    ).toBeInTheDocument()
  })
})
