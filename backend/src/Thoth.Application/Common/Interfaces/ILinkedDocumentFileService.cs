using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface ILinkedDocumentFileService
{
    Task<MarkdownFileValidation> ValidateAsync(string absolutePath, CancellationToken cancellationToken);

    Task<MarkdownFileReadResult> ReadAsync(string absolutePath, CancellationToken cancellationToken);
}
