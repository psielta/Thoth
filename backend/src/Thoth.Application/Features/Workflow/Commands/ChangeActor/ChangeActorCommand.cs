using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow.Commands.ChangeActor;

public sealed record ChangeActorCommand(
    Guid PromptId,
    WorkflowActor Actor,
    string? Note,
    string RowVersion) : IRequest<WorkflowDto>;
