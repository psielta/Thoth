using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow.Queries.GetWorkflowBoard;

public sealed record GetWorkflowBoardQuery(
    PromptWorkflowStatus? WorkflowStatus,
    PromptStatus? PromptStatus,
    Guid? WorkingDirectoryId,
    string? Q) : IRequest<IReadOnlyList<TaskSummaryDto>>;
