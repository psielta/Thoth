using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.Prompts.Commands.CreatePrompt;

public sealed record CreatePromptCommand(
    Guid WorkingDirectoryId,
    Guid? ParentPromptId,
    Guid? FutureTaskId,
    string Title,
    string Content,
    TargetAgent TargetAgent,
    PromptKind Kind,
    PromptStatus Status,
    PromptTemplateKey? SourceTemplateKey,
    IReadOnlyList<FileMentionDto>? Mentions) : IRequest<PromptDto>;
