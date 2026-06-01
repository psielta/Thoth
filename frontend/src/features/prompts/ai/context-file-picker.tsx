import { useQuery } from '@tanstack/react-query'
import { FileText, Loader2, Search, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { searchFiles } from '@/api/files'
import { queryKeys } from '@/api/query-keys'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

type ContextFilePickerProps = {
  workingDirectoryId: string
  value: string[]
  onChange: (value: string[]) => void
}

export function ContextFilePicker({ workingDirectoryId, value, onChange }: ContextFilePickerProps) {
  const [query, setQuery] = useState('')
  const normalizedQuery = query.trim()
  const selected = useMemo(() => new Set(value), [value])

  const filesQuery = useQuery({
    queryKey: queryKeys.files.search(workingDirectoryId, normalizedQuery, 20),
    queryFn: () => searchFiles(workingDirectoryId, normalizedQuery, 20),
    enabled: normalizedQuery.length > 0,
  })

  const results = (filesQuery.data ?? [])
    .filter((file) => !file.isDirectory && !selected.has(file.relativePath))
    .slice(0, 20)

  const addFile = (relativePath: string) => {
    if (selected.has(relativePath)) {
      return
    }

    onChange([...value, relativePath])
    setQuery('')
  }

  const removeFile = (relativePath: string) => {
    onChange(value.filter((path) => path !== relativePath))
  }

  return (
    <div className="grid gap-2">
      <div className="relative">
        <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
        <Input
          aria-label="Buscar arquivos de contexto"
          className="pl-9"
          placeholder="Buscar arquivo no workspace"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
      </div>

      {normalizedQuery.length > 0 ? (
        <div className="max-h-44 overflow-y-auto rounded-md border border-border bg-card">
          {filesQuery.isFetching ? (
            <div className="flex items-center gap-2 px-3 py-2 text-xs text-muted-foreground">
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
              Buscando arquivos
            </div>
          ) : results.length > 0 ? (
            <div role="listbox" aria-label="Resultados de arquivos">
              {results.map((file) => (
                <button
                  key={file.relativePath}
                  type="button"
                  className="flex w-full min-w-0 items-center gap-2 px-3 py-2 text-left text-xs text-foreground transition-colors hover:bg-muted"
                  onClick={() => addFile(file.relativePath)}
                >
                  <FileText className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                  <span className="truncate">{file.relativePath}</span>
                </button>
              ))}
            </div>
          ) : (
            <div className="px-3 py-2 text-xs text-muted-foreground">Nenhum arquivo encontrado.</div>
          )}
        </div>
      ) : null}

      {value.length > 0 ? (
        <div className="flex flex-wrap gap-2" aria-label="Arquivos selecionados">
          {value.map((path) => (
            <span
              key={path}
              className="inline-flex max-w-full items-center gap-1.5 rounded-md border border-border bg-background px-2 py-1 text-xs text-foreground"
            >
              <FileText className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
              <span className="max-w-[18rem] truncate">{path}</span>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-5 w-5"
                aria-label={`Remover ${path}`}
                onClick={() => removeFile(path)}
              >
                <X className="h-3 w-3" />
              </Button>
            </span>
          ))}
        </div>
      ) : null}
    </div>
  )
}
