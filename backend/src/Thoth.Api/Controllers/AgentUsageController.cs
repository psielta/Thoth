using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.AgentUsage.Queries.GetAgentUsage;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AgentUsageController(ISender sender) : ControllerBase
{
    [HttpGet("agent-usage")]
    public async Task<ActionResult<AgentUsageDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAgentUsageQuery(), cancellationToken));
}
