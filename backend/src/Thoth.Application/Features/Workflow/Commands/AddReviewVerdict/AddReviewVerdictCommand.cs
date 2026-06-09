using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.AddReviewVerdict;

public sealed record AddReviewVerdictCommand(
    Guid PromptId,
    string Verdict,
    string RowVersion) : IRequest<WorkflowDto>;
