using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptTasks.Domain.Users;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.ToTable("workflow_templates");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.Id).ValueGeneratedNever();
        builder.Property(template => template.Name).HasMaxLength(160).IsRequired();
        builder.Property(template => template.IsDefault).IsRequired();
        builder.Property(template => template.CreatedAtUtc).IsRequired();
        builder.Property(template => template.UpdatedAtUtc).IsRequired();

        // v1: one template per owner (the default). Prevents duplicates per owner.
        builder.HasIndex(template => template.OwnerId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(template => template.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(template => template.Phases)
            .WithOne(phase => phase.Template)
            .HasForeignKey(phase => phase.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
