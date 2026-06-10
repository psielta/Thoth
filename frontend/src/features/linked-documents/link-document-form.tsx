import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link2, Loader2 } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { z } from 'zod'
import { linkLinkedDocument } from '@/api/linked-documents'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { LinkedDocument } from '@/api/schemas'
import { FormField } from '@/components/form-field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const formSchema = z.object({
  absolutePath: z.string().trim().min(1, 'Informe o caminho completo do markdown.'),
  displayName: z.string().trim().optional(),
})

type FormValues = z.infer<typeof formSchema>

type LinkDocumentFormProps = {
  promptId: string
  onLinked?: (document: LinkedDocument) => void
}

export function LinkDocumentForm({ promptId, onLinked }: LinkDocumentFormProps) {
  const queryClient = useQueryClient()
  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      absolutePath: '',
      displayName: '',
    },
  })

  const linkMutation = useMutation({
    mutationFn: (values: FormValues) =>
      linkLinkedDocument(promptId, {
        absolutePath: values.absolutePath,
        displayName: values.displayName || null,
        documentType: 'ClaudeCodePlan',
      }),
    onSuccess: async (document) => {
      form.reset()
      queryClient.setQueryData(queryKeys.linkedDocuments.detail(document.id), document)
      await queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(promptId) })
      onLinked?.(document)
      toast.success('Markdown vinculado.')
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const onSubmit = form.handleSubmit((values) => linkMutation.mutate(values))

  return (
    <form onSubmit={onSubmit} className="grid gap-3">
      <FormField
        label="Markdown do plano"
        htmlFor="linked-document-path"
        error={form.formState.errors.absolutePath?.message}
      >
        <Input
          id="linked-document-path"
          placeholder="C:\\Users\\psiel\\.claude\\plans\\plano.md"
          autoComplete="off"
          {...form.register('absolutePath')}
        />
      </FormField>

      <FormField label="Nome na tela" htmlFor="linked-document-name">
        <Input
          id="linked-document-name"
          placeholder="Opcional"
          autoComplete="off"
          {...form.register('displayName')}
        />
      </FormField>

      <Button type="submit" disabled={linkMutation.isPending}>
        {linkMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Link2 className="h-4 w-4" />}
        Vincular markdown
      </Button>
    </form>
  )
}
