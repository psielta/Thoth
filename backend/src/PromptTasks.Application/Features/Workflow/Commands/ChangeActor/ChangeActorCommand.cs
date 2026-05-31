using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow.Commands.ChangeActor;

public sealed record ChangeActorCommand(
    Guid PromptId,
    WorkflowActor Actor,
    string? Note,
    string RowVersion) : IRequest<WorkflowDto>;
