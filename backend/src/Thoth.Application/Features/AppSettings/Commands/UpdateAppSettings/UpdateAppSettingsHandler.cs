using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Domain.AppSettings;

namespace Thoth.Application.Features.AppSettings.Commands.UpdateAppSettings;

public sealed class UpdateAppSettingsHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateAppSettingsCommand, AppSettingsDto>
{
    public async Task<AppSettingsDto> Handle(UpdateAppSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = context.AppUserSettings.FirstOrDefault(s => s.OwnerId == currentUser.UserId);
        if (settings is null)
        {
            settings = new AppUserSettings
            {
                OwnerId = currentUser.UserId,
                ShowAgentTerminalOfferAfterChildPrompt = request.ShowAgentTerminalOfferAfterChildPrompt,
            };
            context.Add(settings);
        }
        else
        {
            settings.ShowAgentTerminalOfferAfterChildPrompt = request.ShowAgentTerminalOfferAfterChildPrompt;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new AppSettingsDto(settings.ShowAgentTerminalOfferAfterChildPrompt);
    }
}
