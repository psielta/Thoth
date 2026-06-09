using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.FutureTasks.Commands.CreateFutureTask;
using Thoth.Application.Features.FutureTasks.Commands.DeleteFutureTask;
using Thoth.Application.Features.FutureTasks.Commands.UpdateFutureTask;
using Thoth.Application.Features.FutureTasks.Commands.UpdateFutureTaskStatus;
using Thoth.Application.Features.FutureTasks.Queries.GetFutureTask;
using Thoth.Application.Features.FutureTasks.Queries.GetFutureTasks;
using Thoth.Domain.FutureTasks;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api/future-tasks")]
public sealed class FutureTasksController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FutureTaskDto>>> Get(
        [FromQuery] Guid? workingDirectoryId,
        [FromQuery] FutureTaskStatus? status,
        [FromQuery] bool includeArchived,
        [FromQuery] FutureTaskType? type,
        [FromQuery] string? label,
        [FromQuery] string? q,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new GetFutureTasksQuery(workingDirectoryId, status, includeArchived, type, label, q),
            cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FutureTaskDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetFutureTaskQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<FutureTaskDto>> Create(
        CreateFutureTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateFutureTaskCommand(
                request.WorkingDirectoryId,
                request.Title,
                request.Description,
                request.Type,
                request.Labels,
                request.IssueGithubId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FutureTaskDto>> Update(
        Guid id,
        UpdateFutureTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateFutureTaskCommand(
                id,
                request.Title,
                request.Description,
                request.Type,
                request.Labels,
                request.IssueGithubId,
                request.RowVersion),
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<FutureTaskDto>> UpdateStatus(
        Guid id,
        UpdateFutureTaskStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateFutureTaskStatusCommand(id, request.Status, request.RowVersion), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteFutureTaskCommand(id), cancellationToken);
        return NoContent();
    }

    public sealed record CreateFutureTaskRequest(
        Guid WorkingDirectoryId,
        string Title,
        string Description,
        FutureTaskType Type,
        IReadOnlyList<string>? Labels,
        string? IssueGithubId);

    public sealed record UpdateFutureTaskRequest(
        string Title,
        string Description,
        FutureTaskType Type,
        IReadOnlyList<string>? Labels,
        string? IssueGithubId,
        string RowVersion);

    public sealed record UpdateFutureTaskStatusRequest(FutureTaskStatus Status, string RowVersion);
}
