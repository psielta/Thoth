const rawApiBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

function resolveApiBaseUrl(value: string) {
  const normalized = value.replace(/\/+$/, '')
  if (normalized.startsWith('/')) {
    const origin = typeof window === 'undefined' ? 'http://localhost:5191' : window.location.origin
    return `${origin}${normalized}`
  }

  return normalized
}

export const apiBaseUrl = resolveApiBaseUrl(rawApiBaseUrl ?? 'http://localhost:5191/api')

export const hubUrl = apiBaseUrl.replace(/\/api$/i, '') + '/hubs/prompts'
