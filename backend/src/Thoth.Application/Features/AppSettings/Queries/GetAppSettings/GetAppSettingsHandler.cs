using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.AppSettings.Queries.GetAppSettings;

public sealed class GetAppSettingsHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser)
    : IRequestHandler<GetAppSettingsQuery, AppSettingsDto>
{
    public Task<AppSettingsDto> Handle(GetAppSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = context.AppUserSettings.FirstOrDefault(s => s.OwnerId == currentUser.UserId);
        return Task.FromResult(new AppSettingsDto(
            settings?.ShowAgentTerminalOfferAfterChildPrompt ?? true));
    }
}
