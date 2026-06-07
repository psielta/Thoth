import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { api } from './client'
import { createNotebook, listNotebooks, setNotebookArchived } from './notebooks'

vi.mock('./client', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

const apiMock = api as unknown as {
  get: Mock
  post: Mock
  put: Mock
  delete: Mock
}

function jsonResponse(payload: unknown) {
  return { json: () => Promise.resolve(payload) }
}

const sampleNotebook = {
  id: '019e9f6a-94e7-7a23-965d-c8b05c63ee59',
  title: 'Ideias',
  description: null,
  workingDirectoryId: null,
  workingDirectoryName: null,
  isArchived: false,
  noteCount: 2,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
}

describe('notebooks api', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('lists active notebooks without query params', async () => {
    apiMock.get.mockReturnValue(jsonResponse([sampleNotebook]))

    await expect(listNotebooks()).resolves.toEqual([sampleNotebook])

    const [path, options] = apiMock.get.mock.calls[0]
    expect(path).toBe('notebooks')
    expect((options.searchParams as URLSearchParams).toString()).toBe('')
  })

  it('requests archived notebooks when asked', async () => {
    apiMock.get.mockReturnValue(jsonResponse([sampleNotebook]))

    await listNotebooks(true)

    const [, options] = apiMock.get.mock.calls[0]
    expect((options.searchParams as URLSearchParams).toString()).toBe('includeArchived=true')
  })

  it('creates a notebook', async () => {
    apiMock.post.mockReturnValue(jsonResponse(sampleNotebook))

    await expect(createNotebook({ title: 'Ideias' })).resolves.toMatchObject({ id: sampleNotebook.id })
    expect(apiMock.post).toHaveBeenCalledWith('notebooks', { json: { title: 'Ideias' } })
  })

  it('archives a notebook', async () => {
    apiMock.post.mockReturnValue(jsonResponse({ ...sampleNotebook, isArchived: true }))

    await expect(setNotebookArchived(sampleNotebook.id, true)).resolves.toMatchObject({ isArchived: true })
    expect(apiMock.post).toHaveBeenCalledWith(`notebooks/${sampleNotebook.id}/archive`, {
      json: { isArchived: true },
    })
  })

  it('rejects invalid payloads', async () => {
    apiMock.get.mockReturnValue(jsonResponse([{ id: 'not-a-uuid', title: 'x' }]))

    await expect(listNotebooks()).rejects.toThrow()
  })
})
