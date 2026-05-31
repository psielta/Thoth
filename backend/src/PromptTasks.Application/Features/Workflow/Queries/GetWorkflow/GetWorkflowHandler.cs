using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Queries.GetWorkflow;

public sealed class GetWorkflowHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetWorkflowQuery, WorkflowDto?>
{
    public Task<WorkflowDto?> Handle(GetWorkflowQuery request, CancellationToken cancellationToken)
    {
        // Enforces ownership through the prompt (404 when missing / not owned).
        _ = WorkflowMutationHelpers.GetOwnedPrompt(context, request.PromptId, currentUser.UserId);

        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == request.PromptId);
        if (workflow is null)
        {
            return Task.FromResult<WorkflowDto?>(null);
        }

        var phases = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var events = WorkflowMutationHelpers.LoadEvents(context, workflow.Id);
        return Task.FromResult<WorkflowDto?>(workflow.ToDto(phases, events));
    }
}
