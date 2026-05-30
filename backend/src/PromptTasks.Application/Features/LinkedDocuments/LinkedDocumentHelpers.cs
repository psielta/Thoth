using FluentValidation;
using FluentValidation.Results;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.LinkedDocuments;

internal static class LinkedDocumentHelpers
{
    public static Prompt GetPrompt(IApplicationDbContext context, Guid promptId, Guid ownerId)
    {
        var prompt = context.Prompts.FirstOrDefault(item => item.Id == promptId && item.OwnerId == ownerId);
        return prompt ?? throw new NotFoundException("Prompt was not found.");
    }

    public static (LinkedDocument Document, Prompt Prompt) GetDocument(
        IApplicationDbContext context,
        Guid linkedDocumentId,
        Guid ownerId)
    {
        var document = context.LinkedDocuments.FirstOrDefault(item => item.Id == linkedDocumentId);
        if (document is null)
        {
            throw new NotFoundException("Linked document was not found.");
        }

        var prompt = context.Prompts.FirstOrDefault(item => item.Id == document.PromptId && item.OwnerId == ownerId);
        if (prompt is null)
        {
            throw new NotFoundException("Linked document was not found.");
        }

        return (document, prompt);
    }

    public static LinkedDocumentVersion CreateVersion(
        LinkedDocument document,
        MarkdownFileReadResult readResult,
        LinkedDocumentVersionSource source,
        DateTimeOffset now)
    {
        return new LinkedDocumentVersion
        {
            LinkedDocumentId = document.Id,
            VersionNumber = document.CurrentVersion,
            Content = readResult.Content ?? string.Empty,
            ContentHash = readResult.ContentHash ?? string.Empty,
            SizeBytes = readResult.SizeBytes ?? 0,
            Source = source,
            CreatedAtUtc = now
        };
    }

    public static void ThrowValidation(string propertyName, string message) =>
        throw new ValidationException(new[] { new ValidationFailure(propertyName, message) });

    public static bool HasMarkdownExtension(string path)
    {
        try
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
