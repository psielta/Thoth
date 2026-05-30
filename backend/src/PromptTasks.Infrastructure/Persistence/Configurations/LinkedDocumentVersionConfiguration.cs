using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Infrastructure.Persistence.Configurations;

public sealed class LinkedDocumentVersionConfiguration : IEntityTypeConfiguration<LinkedDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LinkedDocumentVersion> builder)
    {
        builder.ToTable("linked_document_versions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.Id).ValueGeneratedNever();
        builder.Property(version => version.VersionNumber).IsRequired();
        builder.Property(version => version.Content).HasColumnType("text").IsRequired();
        builder.Property(version => version.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(version => version.SizeBytes).IsRequired();
        builder.Property(version => version.Source).HasConversion<int>().IsRequired();
        builder.Property(version => version.CreatedAtUtc).IsRequired();
        builder.HasIndex(version => new { version.LinkedDocumentId, version.VersionNumber }).IsUnique();
        builder.HasIndex(version => new { version.LinkedDocumentId, version.CreatedAtUtc });

        builder.HasOne(version => version.LinkedDocument)
            .WithMany(document => document.Versions)
            .HasForeignKey(version => version.LinkedDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
