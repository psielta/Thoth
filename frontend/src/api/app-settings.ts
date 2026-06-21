import { api } from './client'
import { appSettingsSchema, type AppSettings } from './schemas'

export async function getAppSettings(): Promise<AppSettings> {
  const data = await api.get('app-settings').json()
  return appSettingsSchema.parse(data)
}

export async function updateAppSettings(settings: {
  showAgentTerminalOfferAfterChildPrompt: boolean
}): Promise<AppSettings> {
  const data = await api.put('app-settings', { json: settings }).json()
  return appSettingsSchema.parse(data)
}
