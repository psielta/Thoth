using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Infrastructure.Persistence.Configurations;

public sealed class PromptFileReferenceConfiguration : IEntityTypeConfiguration<PromptFileReference>
{
    public void Configure(EntityTypeBuilder<PromptFileReference> builder)
    {
        builder.ToTable("prompt_file_references");
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Id).ValueGeneratedNever();
        builder.Property(reference => reference.RelativePath).HasMaxLength(1024).IsRequired();
        builder.Property(reference => reference.RawMention).HasMaxLength(1024).IsRequired();
        builder.HasIndex(reference => new { reference.PromptId, reference.RelativePath }).IsUnique();

        builder.HasOne(reference => reference.Prompt)
            .WithMany(prompt => prompt.FileReferences)
            .HasForeignKey(reference => reference.PromptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
