using FluentValidation;

namespace Thoth.Application.Features.Workflow.Commands.AdvancePhase;

public sealed class AdvancePhaseValidator : AbstractValidator<AdvancePhaseCommand>
{
    public AdvancePhaseValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.RowVersion).NotEmpty();
        RuleFor(command => command.Note).MaximumLength(2000);
    }
}
