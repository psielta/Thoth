using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.LinkedDocuments.Commands.LinkDocument;

public sealed record LinkDocumentCommand(
    Guid PromptId,
    string AbsolutePath,
    LinkedDocumentType DocumentType = LinkedDocumentType.ClaudeCodePlan,
    string? DisplayName = null) : IRequest<LinkedDocumentDto>;
