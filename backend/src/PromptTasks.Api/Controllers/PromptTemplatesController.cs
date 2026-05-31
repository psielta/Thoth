using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.PromptTemplates.Queries.GetPromptTemplates;

namespace PromptTasks.Api.Controllers;

[ApiController]
[Route("api/prompt-templates")]
public sealed class PromptTemplatesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromptTemplateDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPromptTemplatesQuery(), cancellationToken));
}
