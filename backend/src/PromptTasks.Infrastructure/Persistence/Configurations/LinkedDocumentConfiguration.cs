using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Infrastructure.Persistence.Configurations;

public sealed class LinkedDocumentConfiguration : IEntityTypeConfiguration<LinkedDocument>
{
    public void Configure(EntityTypeBuilder<LinkedDocument> builder)
    {
        builder.ToTable("linked_documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).ValueGeneratedNever();
        builder.Property(document => document.RelativePath).HasMaxLength(1024).IsRequired();
        builder.Property(document => document.Status).HasConversion<int>().IsRequired();
        builder.Property(document => document.LastContentHash).HasMaxLength(128);
        builder.Property(document => document.CreatedAtUtc).IsRequired();
        builder.Property(document => document.UpdatedAtUtc).IsRequired();

        builder.HasOne(document => document.Prompt)
            .WithMany(prompt => prompt.LinkedDocuments)
            .HasForeignKey(document => document.PromptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(document => document.WorkingDirectory)
            .WithMany()
            .HasForeignKey(document => document.WorkingDirectoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
