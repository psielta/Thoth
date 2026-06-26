import type { Editor } from '@tiptap/core'
import {
  Columns3,
  Heading1,
  Minus,
  Plus,
  Rows3,
  Table2,
  TableProperties,
  Trash2,
} from 'lucide-react'
import type { ReactNode } from 'react'
import { useCallback, useSyncExternalStore } from 'react'
import { cn } from '@/lib/utils'

type TiptapTableToolbarProps = {
  editor: Editor | null
  editable?: boolean
}

type TableActionButtonProps = {
  label: string
  disabled?: boolean
  onClick: () => void
  children: ReactNode
}

function TableActionButton({ label, disabled, onClick, children }: TableActionButtonProps) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      disabled={disabled}
      onClick={onClick}
      className="inline-flex h-7 w-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:cursor-not-allowed disabled:opacity-35"
    >
      {children}
    </button>
  )
}

function StackedTableIcon({ base, action }: { base: ReactNode; action: ReactNode }) {
  return (
    <span className="relative inline-flex h-4 w-4 items-center justify-center">
      {base}
      <span className="absolute -right-1 -top-1 inline-flex h-3 w-3 items-center justify-center rounded-full bg-background text-foreground">
        {action}
      </span>
    </span>
  )
}

export function TiptapTableToolbar({ editor, editable = true }: TiptapTableToolbarProps) {
  const subscribeToEditor = useCallback(
    (onStoreChange: () => void) => {
      if (!editor) {
        return () => undefined
      }

      editor.on('selectionUpdate', onStoreChange)
      editor.on('transaction', onStoreChange)

      return () => {
        editor.off('selectionUpdate', onStoreChange)
        editor.off('transaction', onStoreChange)
      }
    },
    [editor],
  )

  const getEditorSnapshot = useCallback(() => editor?.isActive('table') ?? false, [editor])
  const isInTable = useSyncExternalStore(subscribeToEditor, getEditorSnapshot, () => false)

  if (!editable) {
    return null
  }

  const tableActionDisabled = !editor || !isInTable

  return (
    <div
      className={cn(
        'flex items-center gap-0.5 rounded border border-border bg-card px-1 py-0.5 normal-case shadow-sm',
        !editor && 'opacity-60',
      )}
      aria-label="Ferramentas de tabela"
    >
      <TableActionButton
        label="Inserir tabela"
        disabled={!editor}
        onClick={() => editor?.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()}
      >
        <Table2 className="h-4 w-4" />
      </TableActionButton>
      <span className="mx-1 h-4 w-px bg-border" />
      <TableActionButton
        label="Adicionar linha"
        disabled={tableActionDisabled}
        onClick={() => editor?.chain().focus().addRowAfter().run()}
      >
        <StackedTableIcon base={<Rows3 className="h-4 w-4" />} action={<Plus className="h-2.5 w-2.5" />} />
      </TableActionButton>
      <TableActionButton
        label="Remover linha"
        disabled={tableActionDisabled}
        onClick={() => editor?.chain().focus().deleteRow().run()}
      >
        <StackedTableIcon base={<Rows3 className="h-4 w-4" />} action={<Minus className="h-2.5 w-2.5" />} />
      </TableActionButton>
      <TableActionButton
        label="Adicionar coluna"
        disabled={tableActionDisabled}
        onClick={() => editor?.chain().focus().addColumnAfter().run()}
      >
        <StackedTableIcon base={<Columns3 className="h-4 w-4" />} action={<Plus className="h-2.5 w-2.5" />} />
      </TableActionButton>
      <TableActionButton
        label="Remover coluna"
        disabled={tableActionDisabled}
        onClick={() => editor?.chain().focus().deleteColumn().run()}
      >
        <StackedTableIcon base={<Columns3 className="h-4 w-4" />} action={<Minus className="h-2.5 w-2.5" />} />
      </TableActionButton>
      <TableActionButton
        label="Alternar cabecalho"
        disabled={tableActionDisabled}
        onClick={() => editor?.chain().focus().toggleHeaderRow().run()}
      >
        <StackedTableIcon
          base={<TableProperties className="h-4 w-4" />}
          action={<Heading1 className="h-2.5 w-2.5" />}
        />
      </TableActionButton>
      <TableActionButton
        label="Excluir tabela"
        disabled={tableActionDisabled}
        onClick={() => editor?.chain().focus().deleteTable().run()}
      >
        <Trash2 className="h-4 w-4" />
      </TableActionButton>
    </div>
  )
}
