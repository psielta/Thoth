using FluentValidation;

namespace PromptTasks.Application.Features.Workflow.Commands.SetPhase;

public sealed class SetPhaseValidator : AbstractValidator<SetPhaseCommand>
{
    public SetPhaseValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.PhaseId).NotEmpty();
        RuleFor(command => command.RowVersion).NotEmpty();
        RuleFor(command => command.Actor).IsInEnum().When(command => command.Actor.HasValue);
        RuleFor(command => command.Note).MaximumLength(2000);
    }
}
