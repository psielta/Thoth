using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.LinkedDocuments.Commands.LinkDocument;
using Thoth.Application.Features.LinkedDocuments.Commands.PauseLinkedDocument;
using Thoth.Application.Features.LinkedDocuments.Commands.RefreshLinkedDocument;
using Thoth.Application.Features.LinkedDocuments.Commands.RemoveLinkedDocument;
using Thoth.Application.Features.LinkedDocuments.Commands.ResumeLinkedDocument;
using Thoth.Application.Features.LinkedDocuments.Commands.SetLinkedDocumentPullRequest;
using Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocument;
using Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentContent;
using Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocuments;
using Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentVersions;
using Thoth.Application.Features.PromptTemplates.Commands.GeneratePromptDraft;
using Thoth.Domain.Prompts;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class LinkedDocumentsController(ISender sender) : ControllerBase
{
    [HttpGet("prompts/{promptId:guid}/linked-documents")]
    public async Task<ActionResult<IReadOnlyList<LinkedDocumentDto>>> GetForPrompt(
        Guid promptId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLinkedDocumentsQuery(promptId), cancellationToken));

    [HttpPost("prompts/{promptId:guid}/linked-documents")]
    public async Task<ActionResult<LinkedDocumentDto>> Link(
        Guid promptId,
        LinkDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LinkDocumentCommand(promptId, request.AbsolutePath, request.DocumentType, request.DisplayName),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("linked-documents/{id:guid}")]
    public async Task<ActionResult<LinkedDocumentDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLinkedDocumentQuery(id), cancellationToken));

    [HttpGet("linked-documents/{id:guid}/content")]
    public async Task<ActionResult<LinkedDocumentContentDto>> GetContent(
        Guid id,
        [FromQuery] int? version,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLinkedDocumentContentQuery(id, version), cancellationToken));

    [HttpGet("linked-documents/{id:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<LinkedDocumentVersionDto>>> GetVersions(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLinkedDocumentVersionsQuery(id), cancellationToken));

    [HttpPost("linked-documents/{id:guid}/pause")]
    public async Task<ActionResult<LinkedDocumentDto>> Pause(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PauseLinkedDocumentCommand(id), cancellationToken));

    [HttpPost("linked-documents/{id:guid}/resume")]
    public async Task<ActionResult<LinkedDocumentDto>> Resume(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ResumeLinkedDocumentCommand(id), cancellationToken));

    [HttpPost("linked-documents/{id:guid}/refresh")]
    public async Task<ActionResult<LinkedDocumentDto>> Refresh(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RefreshLinkedDocumentCommand(id), cancellationToken));

    [HttpPut("linked-documents/{id:guid}/pull-request")]
    public async Task<ActionResult<LinkedDocumentDto>> SetPullRequest(
        Guid id,
        SetPullRequestRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SetLinkedDocumentPullRequestCommand(id, request.PullRequest), cancellationToken));

    [HttpPost("linked-documents/{id:guid}/prompt-drafts")]
    public async Task<ActionResult<GeneratedPromptDraftDto>> GeneratePromptDraft(
        Guid id,
        GeneratePromptDraftRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new GeneratePromptDraftCommand(id, request.TemplateKey, request.PullRequest, request.Inputs),
            cancellationToken));

    [HttpDelete("linked-documents/{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveLinkedDocumentCommand(id), cancellationToken);
        return NoContent();
    }

    public sealed record LinkDocumentRequest(
        string AbsolutePath,
        LinkedDocumentType DocumentType = LinkedDocumentType.ClaudeCodePlan,
        string? DisplayName = null);

    public sealed record GeneratePromptDraftRequest(
        PromptTemplateKey TemplateKey,
        string? PullRequest = null,
        IReadOnlyDictionary<string, string>? Inputs = null);

    public sealed record SetPullRequestRequest(string? PullRequest);
}
