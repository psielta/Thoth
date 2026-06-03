const rawApiBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

export const apiBaseUrl = (rawApiBaseUrl ?? 'http://localhost:5191/api').replace(/\/+$/, '')

export const hubUrl = apiBaseUrl.replace(/\/api$/i, '') + '/hubs/prompts'
