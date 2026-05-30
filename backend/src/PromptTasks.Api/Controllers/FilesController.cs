using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Files.Queries.SearchFiles;
using PromptTasks.Application.Features.Files.Queries.ValidateFileReferences;

namespace PromptTasks.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(ISender sender) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<FileSearchResultDto>>> Search(
        [FromQuery] Guid workingDirectoryId,
        [FromQuery] string query = "",
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new SearchFilesQuery(workingDirectoryId, query, limit), cancellationToken));

    [HttpPost("validate-references")]
    public async Task<ActionResult<IReadOnlyList<FileReferenceValidationDto>>> ValidateReferences(
        ValidateFileReferencesRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(
            new ValidateFileReferencesQuery(request.WorkingDirectoryId, request.RelativePaths),
            cancellationToken));

    public sealed record ValidateFileReferencesRequest(Guid WorkingDirectoryId, IReadOnlyList<string> RelativePaths);
}
