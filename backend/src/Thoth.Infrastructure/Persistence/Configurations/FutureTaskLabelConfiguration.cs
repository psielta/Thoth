using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thoth.Domain.FutureTasks;

namespace Thoth.Infrastructure.Persistence.Configurations;

public sealed class FutureTaskLabelConfiguration : IEntityTypeConfiguration<FutureTaskLabel>
{
    public void Configure(EntityTypeBuilder<FutureTaskLabel> builder)
    {
        builder.ToTable("future_task_labels");
        builder.HasKey(label => label.Id);
        builder.Property(label => label.Id).ValueGeneratedNever();
        builder.Property(label => label.Label).HasMaxLength(64).IsRequired();
        builder.HasIndex(label => new { label.FutureTaskId, label.Label }).IsUnique();

        builder.HasOne(label => label.FutureTask)
            .WithMany(task => task.Labels)
            .HasForeignKey(label => label.FutureTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
