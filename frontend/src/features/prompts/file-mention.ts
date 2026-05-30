import { Mention } from '@tiptap/extension-mention'
import type { SuggestionKeyDownProps, SuggestionOptions, SuggestionProps } from '@tiptap/suggestion'
import type { FileSearchResult } from '@/api/schemas'

type MentionAttrs = {
  id: string | null
  label?: string | null
}

type SearchMentions = (query: string) => Promise<FileSearchResult[]>

export const FileMention = Mention.extend({
  renderMarkdown({ node }) {
    return `@${node.attrs.id}`
  },
})

export function createFileMentionSuggestion(searchMentions: SearchMentions) {
  return {
    char: '@',
    allowedPrefixes: null,
    items: ({ query }) => searchMentions(query),
    command: ({ editor, range, props }) => {
      if (!props.id) {
        return
      }

      editor
        .chain()
        .focus()
        .insertContentAt(range, [
          {
            type: 'mention',
            attrs: {
              id: props.id,
              label: props.label,
              mentionSuggestionChar: '@',
            },
          },
          {
            type: 'text',
            text: ' ',
          },
        ])
        .run()
    },
    render: () => {
      let container: HTMLDivElement | null = null
      let selectedIndex = 0
      let lastProps: SuggestionProps<FileSearchResult, MentionAttrs> | null = null

      const updatePosition = (props: SuggestionProps<FileSearchResult, MentionAttrs>) => {
        if (!container || !props.clientRect) {
          return
        }

        const rect = props.clientRect()
        if (!rect) {
          return
        }

        container.style.left = `${Math.min(rect.left, window.innerWidth - container.offsetWidth - 16)}px`
        container.style.top = `${rect.bottom + 8}px`
      }

      const selectItem = (index: number) => {
        const item = lastProps?.items[index]
        if (!item || !lastProps) {
          return
        }

        lastProps.command({
          id: item.relativePath,
          label: item.relativePath,
        })
      }

      const renderItems = (props: SuggestionProps<FileSearchResult, MentionAttrs>) => {
        lastProps = props
        selectedIndex = Math.min(selectedIndex, Math.max(props.items.length - 1, 0))

        if (!container) {
          return
        }

        container.replaceChildren()

        if (!props.items.length) {
          const empty = document.createElement('div')
          empty.className = 'px-3 py-2 text-sm text-[#66746b]'
          empty.textContent = 'Nenhum arquivo encontrado'
          container.appendChild(empty)
          updatePosition(props)
          return
        }

        props.items.forEach((item, index) => {
          const button = document.createElement('button')
          button.type = 'button'
          button.dataset.selected = String(index === selectedIndex)
          button.addEventListener('mousedown', (event) => {
            event.preventDefault()
            selectItem(index)
          })

          const name = document.createElement('span')
          name.textContent = item.fileName
          const path = document.createElement('small')
          path.textContent = item.relativePath

          button.append(name, path)
          container?.appendChild(button)
        })

        updatePosition(props)
      }

      const moveSelection = (delta: number) => {
        const count = lastProps?.items.length ?? 0
        if (!count) {
          return
        }

        selectedIndex = (selectedIndex + delta + count) % count
        if (lastProps) {
          renderItems(lastProps)
        }
      }

      return {
        onStart: (props) => {
          container = document.createElement('div')
          container.className = 'mention-suggestion'
          document.body.appendChild(container)
          renderItems(props)
        },
        onUpdate: (props) => {
          renderItems(props)
        },
        onKeyDown: ({ event }: SuggestionKeyDownProps) => {
          if (event.key === 'ArrowDown') {
            moveSelection(1)
            return true
          }

          if (event.key === 'ArrowUp') {
            moveSelection(-1)
            return true
          }

          if (event.key === 'Enter') {
            selectItem(selectedIndex)
            return true
          }

          return false
        },
        onExit: () => {
          container?.remove()
          container = null
          lastProps = null
        },
      }
    },
  } satisfies Omit<SuggestionOptions<FileSearchResult, MentionAttrs>, 'editor'>
}
