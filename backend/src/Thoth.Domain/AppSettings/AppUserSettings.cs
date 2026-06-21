using Thoth.Domain.Common;

namespace Thoth.Domain.AppSettings;

public sealed class AppUserSettings : AuditableEntity
{
    public bool ShowAgentTerminalOfferAfterChildPrompt { get; set; } = true;
}
