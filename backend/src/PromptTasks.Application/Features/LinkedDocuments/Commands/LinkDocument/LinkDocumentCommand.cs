using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.LinkDocument;

public sealed record LinkDocumentCommand(
    Guid PromptId,
    string AbsolutePath,
    LinkedDocumentType DocumentType = LinkedDocumentType.ClaudeCodePlan,
    string? DisplayName = null) : IRequest<LinkedDocumentDto>;
