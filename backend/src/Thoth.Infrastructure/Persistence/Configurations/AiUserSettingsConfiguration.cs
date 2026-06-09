using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thoth.Domain.Ai;

namespace Thoth.Infrastructure.Persistence.Configurations;

public sealed class AiUserSettingsConfiguration : IEntityTypeConfiguration<AiUserSettings>
{
    public void Configure(EntityTypeBuilder<AiUserSettings> builder)
    {
        builder.ToTable("ai_user_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Model).HasMaxLength(100).IsRequired();
        builder.Property(s => s.ThinkingLevel).HasMaxLength(50);
        builder.HasIndex(s => s.OwnerId).IsUnique();
    }
}
