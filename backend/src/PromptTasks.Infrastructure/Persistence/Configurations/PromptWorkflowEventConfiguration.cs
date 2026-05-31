using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Infrastructure.Persistence.Configurations;

public sealed class PromptWorkflowEventConfiguration : IEntityTypeConfiguration<PromptWorkflowEvent>
{
    public void Configure(EntityTypeBuilder<PromptWorkflowEvent> builder)
    {
        builder.ToTable("prompt_workflow_events");
        builder.HasKey(@event => @event.Id);
        builder.Property(@event => @event.Id).ValueGeneratedNever();
        builder.Property(@event => @event.Type).HasConversion<int>().IsRequired();
        builder.Property(@event => @event.PhaseNameSnapshot).HasMaxLength(120);
        builder.Property(@event => @event.Actor).HasConversion<int>();
        builder.Property(@event => @event.Note).HasColumnType("text");
        builder.Property(@event => @event.OccurredAtUtc).IsRequired();

        builder.HasIndex(@event => new { @event.PromptWorkflowId, @event.OccurredAtUtc });
    }
}
