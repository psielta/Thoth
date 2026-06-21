using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.AppSettings.Commands.UpdateAppSettings;

public sealed record UpdateAppSettingsCommand(
    bool ShowAgentTerminalOfferAfterChildPrompt) : IRequest<AppSettingsDto>;
