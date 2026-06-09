using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.Prompts.Commands.UpdatePrompt;

public sealed record UpdatePromptCommand(
    Guid Id,
    string Title,
    string Content,
    TargetAgent TargetAgent,
    PromptKind Kind,
    PromptStatus Status,
    string RowVersion,
    IReadOnlyList<FileMentionDto>? Mentions) : IRequest<PromptDto>;
