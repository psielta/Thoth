using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thoth.Domain.Ai;

namespace Thoth.Infrastructure.Persistence.Configurations;

public sealed class AiChatMessageConfiguration : IEntityTypeConfiguration<AiChatMessage>
{
    public void Configure(EntityTypeBuilder<AiChatMessage> builder)
    {
        builder.ToTable("ai_chat_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Role).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Content).HasColumnType("text").IsRequired();
        builder.HasIndex(m => new { m.SessionId, m.Sequence });
        builder.HasOne(m => m.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
