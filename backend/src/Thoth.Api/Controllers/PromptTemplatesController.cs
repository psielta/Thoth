using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.PromptTemplates.Queries.GetPromptTemplates;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api/prompt-templates")]
public sealed class PromptTemplatesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromptTemplateDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPromptTemplatesQuery(), cancellationToken));
}
