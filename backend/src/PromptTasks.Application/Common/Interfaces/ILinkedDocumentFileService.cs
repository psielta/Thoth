using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Common.Interfaces;

public interface ILinkedDocumentFileService
{
    Task<MarkdownFileValidation> ValidateAsync(string absolutePath, CancellationToken cancellationToken);

    Task<MarkdownFileReadResult> ReadAsync(string absolutePath, CancellationToken cancellationToken);
}
