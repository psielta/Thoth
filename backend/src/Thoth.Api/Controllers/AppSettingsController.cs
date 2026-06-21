using MediatR;
using Microsoft.AspNetCore.Mvc;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.AppSettings.Commands.UpdateAppSettings;
using Thoth.Application.Features.AppSettings.Queries.GetAppSettings;

namespace Thoth.Api.Controllers;

[ApiController]
[Route("api/app-settings")]
public sealed class AppSettingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AppSettingsDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAppSettingsQuery(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<AppSettingsDto>> Update(
        UpdateAppSettingsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new UpdateAppSettingsCommand(request.ShowAgentTerminalOfferAfterChildPrompt),
            cancellationToken));

    public sealed record UpdateAppSettingsRequest(bool ShowAgentTerminalOfferAfterChildPrompt);
}
