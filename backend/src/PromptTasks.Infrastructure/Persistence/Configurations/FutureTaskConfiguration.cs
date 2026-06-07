using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptTasks.Domain.FutureTasks;

namespace PromptTasks.Infrastructure.Persistence.Configurations;

public sealed class FutureTaskConfiguration : IEntityTypeConfiguration<FutureTask>
{
    public void Configure(EntityTypeBuilder<FutureTask> builder)
    {
        builder.ToTable("future_tasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.Title).HasMaxLength(220).IsRequired();
        builder.Property(task => task.Description).HasColumnType("text").IsRequired();
        builder.Property(task => task.Status).HasConversion<int>().IsRequired();
        builder.Property(task => task.Type).HasConversion<int>().IsRequired();
        builder.Property(task => task.IssueGithubId).HasMaxLength(64);
        builder.Property(task => task.CreatedAtUtc).IsRequired();
        builder.Property(task => task.UpdatedAtUtc).IsRequired();
        builder.Property(task => task.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(task => new { task.WorkingDirectoryId, task.Status });
        builder.HasIndex(task => new { task.WorkingDirectoryId, task.UpdatedAtUtc });

        builder.HasOne(task => task.Owner)
            .WithMany()
            .HasForeignKey(task => task.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.WorkingDirectory)
            .WithMany()
            .HasForeignKey(task => task.WorkingDirectoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
