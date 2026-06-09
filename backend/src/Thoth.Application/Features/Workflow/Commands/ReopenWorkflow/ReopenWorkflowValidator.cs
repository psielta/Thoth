using FluentValidation;

namespace Thoth.Application.Features.Workflow.Commands.ReopenWorkflow;

public sealed class ReopenWorkflowValidator : AbstractValidator<ReopenWorkflowCommand>
{
    public ReopenWorkflowValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.RowVersion).NotEmpty();
        RuleFor(command => command.PhaseId).NotEqual(Guid.Empty).When(command => command.PhaseId.HasValue);
    }
}
