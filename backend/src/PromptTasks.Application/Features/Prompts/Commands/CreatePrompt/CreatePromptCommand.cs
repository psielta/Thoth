using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.Prompts.Commands.CreatePrompt;

public sealed record CreatePromptCommand(
    Guid WorkingDirectoryId,
    Guid? ParentPromptId,
    string Title,
    string Content,
    TargetAgent TargetAgent,
    PromptKind Kind,
    PromptStatus Status,
    IReadOnlyList<FileMentionDto>? Mentions) : IRequest<PromptDto>;
