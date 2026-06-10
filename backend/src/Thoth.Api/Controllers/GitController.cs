using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Git.Queries.GetGitDiff;
using Thoth.Application.Features.Git.Queries.GetGitStatus;
using Thoth.Application.Features.Git.Queries.GetOriginalFileContent;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api/git")]
public sealed class GitController(ISender sender) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<IReadOnlyList<GitFileStatusDto>>> GetStatus(
        [FromQuery] Guid workingDirectoryId,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetGitStatusQuery(workingDirectoryId), cancellationToken));

    [HttpGet("original-file")]
    public async Task<ActionResult<GitOriginalFileDto>> GetOriginalFile(
        [FromQuery] Guid workingDirectoryId,
        [FromQuery] string path,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetOriginalFileContentQuery(workingDirectoryId, path), cancellationToken));

    [HttpGet("diff")]
    public async Task<ActionResult<GitDiffDto>> GetDiff(
        [FromQuery] Guid workingDirectoryId,
        [FromQuery] string path,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetGitDiffQuery(workingDirectoryId, path), cancellationToken));
}
