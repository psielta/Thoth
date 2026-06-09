using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Files.Queries.BrowseDirectory;
using Thoth.Application.Features.Files.Queries.ReadFileContent;
using Thoth.Application.Features.Files.Queries.SearchFiles;
using Thoth.Application.Features.Files.Queries.ValidateFileReferences;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(ISender sender) : ControllerBase
{
    [HttpGet("tree")]
    public async Task<ActionResult<IReadOnlyList<DirectoryEntryDto>>> BrowseTree(
        [FromQuery] Guid workingDirectoryId,
        [FromQuery] string relativePath = "",
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new BrowseDirectoryQuery(workingDirectoryId, relativePath), cancellationToken));

    [HttpGet("content")]
    public async Task<ActionResult<FileContentDto>> GetContent(
        [FromQuery] Guid workingDirectoryId,
        [FromQuery] string relativePath,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new ReadFileContentQuery(workingDirectoryId, relativePath), cancellationToken));

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
