import { api } from './client'
import { terminalCapabilitiesSchema, terminalSessionSchema } from './schemas'
import { z } from 'zod'

export async function getTerminalCapabilities() {
  const data = await api.get('terminals/capabilities').json<unknown>()
  return terminalCapabilitiesSchema.parse(data)
}

export async function listTerminals(promptId: string) {
  const data = await api.get(`prompts/${promptId}/terminals`).json<unknown>()
  return z.array(terminalSessionSchema).parse(data)
}

export async function createTerminal(promptId: string, shell?: string) {
  const data = await api.post(`prompts/${promptId}/terminals`, { json: { shell } }).json<unknown>()
  return terminalSessionSchema.parse(data)
}

export async function closeTerminal(sessionId: string) {
  await api.delete(`terminals/${sessionId}`)
}