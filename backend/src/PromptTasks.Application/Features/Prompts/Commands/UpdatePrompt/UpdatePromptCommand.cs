using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.Prompts.Commands.UpdatePrompt;

public sealed record UpdatePromptCommand(
    Guid Id,
    string Title,
    string Content,
    TargetAgent TargetAgent,
    PromptKind Kind,
    PromptStatus Status,
    string RowVersion,
    IReadOnlyList<FileMentionDto>? Mentions) : IRequest<PromptDto>;
