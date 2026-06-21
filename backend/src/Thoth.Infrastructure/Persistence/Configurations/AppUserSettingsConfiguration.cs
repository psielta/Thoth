using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thoth.Domain.AppSettings;

namespace Thoth.Infrastructure.Persistence.Configurations;

public sealed class AppUserSettingsConfiguration : IEntityTypeConfiguration<AppUserSettings>
{
    public void Configure(EntityTypeBuilder<AppUserSettings> builder)
    {
        builder.ToTable("app_user_settings");
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.ShowAgentTerminalOfferAfterChildPrompt).HasDefaultValue(true);
        builder.HasIndex(settings => settings.OwnerId).IsUnique();
    }
}
