import ky, { isHTTPError, type HTTPError } from 'ky'
import { apiBaseUrl } from '@/env'

export const api = ky.create({
  prefix: apiBaseUrl,
  timeout: 20_000,
  hooks: {
    beforeError: [
      async ({ error }) => {
        if (isHTTPError(error)) {
          error.message = await getApiErrorMessage(error)
        }

        return error
      },
    ],
  },
})

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function getApiErrorMessage(error: HTTPError) {
  try {
    const problem = (await error.response.json()) as ProblemDetails
    const validationMessage = problem.errors
      ? Object.values(problem.errors).flat().filter(Boolean).join(' ')
      : undefined

    return validationMessage || problem.detail || problem.title || error.message
  } catch {
    return error.message
  }
}

export function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message
  }

  return 'Operacao nao concluida.'
}
