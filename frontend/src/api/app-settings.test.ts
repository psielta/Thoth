import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { api } from './client'
import { getAppSettings, updateAppSettings } from './app-settings'

vi.mock('./client', () => ({
  api: {
    get: vi.fn(),
    put: vi.fn(),
  },
}))

const apiMock = api as unknown as {
  get: Mock
  put: Mock
}

function jsonResponse(payload: unknown) {
  return { json: () => Promise.resolve(payload) }
}

describe('app settings api', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('gets app settings', async () => {
    apiMock.get.mockReturnValue(jsonResponse({ showAgentTerminalOfferAfterChildPrompt: true }))

    await expect(getAppSettings()).resolves.toEqual({ showAgentTerminalOfferAfterChildPrompt: true })
    expect(apiMock.get).toHaveBeenCalledWith('app-settings')
  })

  it('updates app settings', async () => {
    apiMock.put.mockReturnValue(jsonResponse({ showAgentTerminalOfferAfterChildPrompt: false }))

    await expect(
      updateAppSettings({ showAgentTerminalOfferAfterChildPrompt: false }),
    ).resolves.toEqual({ showAgentTerminalOfferAfterChildPrompt: false })
    expect(apiMock.put).toHaveBeenCalledWith('app-settings', {
      json: { showAgentTerminalOfferAfterChildPrompt: false },
    })
  })
})
