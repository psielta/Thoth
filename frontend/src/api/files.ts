import { api } from './client'
import { fileSearchResultListSchema } from './schemas'

export async function searchFiles(workingDirectoryId: string, query: string, limit = 30) {
  const searchParams = new URLSearchParams({
    workingDirectoryId,
    query,
    limit: limit.toString(),
  })

  const data = await api.get('files/search', { searchParams }).json<unknown>()
  return fileSearchResultListSchema.parse(data)
}
