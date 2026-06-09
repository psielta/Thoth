using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.Prompts.Queries.GetPrompts;

public sealed record GetPromptsQuery(
    Guid? WorkingDirectoryId,
    Guid? ParentPromptId,
    bool RootOnly,
    PromptStatus? Status,
    TargetAgent? Agent,
    PromptKind? Kind,
    string? Q,
    Guid? FutureTaskId = null) : IRequest<IReadOnlyList<PromptDto>>;
