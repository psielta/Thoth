using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.AddReviewVerdict;

public sealed record AddReviewVerdictCommand(
    Guid PromptId,
    string Verdict,
    string RowVersion) : IRequest<WorkflowDto>;
