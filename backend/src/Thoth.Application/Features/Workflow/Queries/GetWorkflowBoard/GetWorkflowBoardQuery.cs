using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow.Queries.GetWorkflowBoard;

public sealed record GetWorkflowBoardQuery(
    PromptWorkflowStatus? WorkflowStatus,
    PromptStatus? PromptStatus,
    Guid? WorkingDirectoryId,
    string? Q) : IRequest<IReadOnlyList<TaskSummaryDto>>;
