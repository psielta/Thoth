using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Workflow.Commands.AddReviewVerdict;
using Thoth.Application.Features.Workflow.Commands.AddWorkflowNote;
using Thoth.Application.Features.Workflow.Commands.AdvancePhase;
using Thoth.Application.Features.Workflow.Commands.ChangeActor;
using Thoth.Application.Features.Workflow.Commands.CompleteWorkflow;
using Thoth.Application.Features.Workflow.Commands.ReopenWorkflow;
using Thoth.Application.Features.Workflow.Commands.SetPhase;
using Thoth.Application.Features.Workflow.Commands.StartWorkflow;
using Thoth.Application.Features.Workflow.Commands.UpdateTaskPhases;
using Thoth.Application.Features.Workflow.Commands.UpdateWorkflowTemplate;
using Thoth.Application.Features.Workflow.Queries.GetWorkflow;
using Thoth.Application.Features.Workflow.Queries.GetWorkflowBoard;
using Thoth.Application.Features.Workflow.Queries.GetWorkflowTemplate;
using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class WorkflowController(ISender sender) : ControllerBase
{
    [HttpGet("workflow/board")]
    public async Task<ActionResult<IReadOnlyList<TaskSummaryDto>>> GetBoard(
        [FromQuery] PromptWorkflowStatus? workflowStatus,
        [FromQuery] PromptStatus? promptStatus,
        [FromQuery] Guid? workingDirectoryId,
        [FromQuery] string? q,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetWorkflowBoardQuery(workflowStatus, promptStatus, workingDirectoryId, q), cancellationToken));

    [HttpGet("workflow/template")]
    public async Task<ActionResult<WorkflowTemplateDto>> GetTemplate(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetWorkflowTemplateQuery(), cancellationToken));

    [HttpPut("workflow/template")]
    public async Task<ActionResult<WorkflowTemplateDto>> UpdateTemplate(
        UpdateTemplateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateWorkflowTemplateCommand(request.Phases), cancellationToken));

    [HttpGet("prompts/{promptId:guid}/workflow")]
    public async Task<ActionResult<WorkflowDto>> Get(Guid promptId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetWorkflowQuery(promptId), cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow")]
    public async Task<ActionResult<WorkflowDto>> Start(
        Guid promptId,
        [FromBody] StartRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new StartWorkflowCommand(promptId, request?.InitialPhaseOrderIndex), cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow/advance")]
    public async Task<ActionResult<WorkflowDto>> Advance(
        Guid promptId,
        AdvanceRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AdvancePhaseCommand(promptId, request.RowVersion, request.Note), cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow/phase")]
    public async Task<ActionResult<WorkflowDto>> SetPhase(
        Guid promptId,
        SetPhaseRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new SetPhaseCommand(promptId, request.PhaseId, request.Actor, request.Note, request.RowVersion),
            cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow/actor")]
    public async Task<ActionResult<WorkflowDto>> ChangeActor(
        Guid promptId,
        ChangeActorRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new ChangeActorCommand(promptId, request.Actor, request.Note, request.RowVersion),
            cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow/notes")]
    public async Task<ActionResult<WorkflowDto>> AddNote(
        Guid promptId,
        AddNoteRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AddWorkflowNoteCommand(promptId, request.Note), cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow/review-verdict")]
    public async Task<ActionResult<WorkflowDto>> AddReviewVerdict(
        Guid promptId,
        AddReviewVerdictRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new AddReviewVerdictCommand(promptId, request.Verdict, request.RowVersion),
            cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow/complete")]
    public async Task<ActionResult<WorkflowDto>> Complete(
        Guid promptId,
        CompleteRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CompleteWorkflowCommand(promptId, request.Note, request.RowVersion), cancellationToken));

    [HttpPost("prompts/{promptId:guid}/workflow/reopen")]
    public async Task<ActionResult<WorkflowDto>> Reopen(
        Guid promptId,
        ReopenRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReopenWorkflowCommand(promptId, request.PhaseId, request.RowVersion), cancellationToken));

    [HttpPut("prompts/{promptId:guid}/workflow/phases")]
    public async Task<ActionResult<WorkflowDto>> UpdatePhases(
        Guid promptId,
        UpdatePhasesRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateTaskPhasesCommand(promptId, request.Phases, request.RowVersion), cancellationToken));

    public sealed record StartRequest(int? InitialPhaseOrderIndex);
    public sealed record AdvanceRequest(string RowVersion, string? Note);
    public sealed record SetPhaseRequest(Guid PhaseId, WorkflowActor? Actor, string? Note, string RowVersion);
    public sealed record ChangeActorRequest(WorkflowActor Actor, string? Note, string RowVersion);
    public sealed record AddNoteRequest(string Note);
    public sealed record AddReviewVerdictRequest(string Verdict, string RowVersion);
    public sealed record CompleteRequest(string? Note, string RowVersion);
    public sealed record ReopenRequest(Guid? PhaseId, string RowVersion);
    public sealed record UpdatePhasesRequest(IReadOnlyList<WorkflowPhaseInput> Phases, string RowVersion);
    public sealed record UpdateTemplateRequest(IReadOnlyList<WorkflowPhaseInput> Phases);
}
