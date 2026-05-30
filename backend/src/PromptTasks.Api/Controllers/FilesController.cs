using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Files.Queries.SearchFiles;

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
}
