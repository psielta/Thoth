using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.Prompts.Queries.GetPrompts;

public sealed record GetPromptsQuery(
    Guid? WorkingDirectoryId,
    Guid? ParentPromptId,
    bool RootOnly,
    PromptStatus? Status,
    TargetAgent? Agent,
    PromptKind? Kind,
    string? Q) : IRequest<IReadOnlyList<PromptDto>>;
