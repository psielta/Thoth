using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Infrastructure.Persistence.Configurations;

public sealed class PromptVersionConfiguration : IEntityTypeConfiguration<PromptVersion>
{
    public void Configure(EntityTypeBuilder<PromptVersion> builder)
    {
        builder.ToTable("prompt_versions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.Id).ValueGeneratedNever();
        builder.Property(version => version.Title).HasMaxLength(220).IsRequired();
        builder.Property(version => version.Content).HasColumnType("text").IsRequired();
        builder.Property(version => version.TargetAgent).HasConversion<int>().IsRequired();
        builder.Property(version => version.Kind).HasConversion<int>().IsRequired();
        builder.Property(version => version.Status).HasConversion<int>().IsRequired();
        builder.Property(version => version.ChangeNote).HasMaxLength(500);
        builder.Property(version => version.CreatedAtUtc).IsRequired();
        builder.HasIndex(version => new { version.PromptId, version.VersionNumber }).IsUnique();

        builder.HasOne(version => version.Prompt)
            .WithMany(prompt => prompt.Versions)
            .HasForeignKey(version => version.PromptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
