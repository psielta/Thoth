using FluentValidation;

namespace PromptTasks.Application.Features.Workflow.Commands.ChangeActor;

public sealed class ChangeActorValidator : AbstractValidator<ChangeActorCommand>
{
    public ChangeActorValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.Actor).IsInEnum();
        RuleFor(command => command.RowVersion).NotEmpty();
        RuleFor(command => command.Note).MaximumLength(2000);
    }
}
