using FluentValidation;

namespace PromptTasks.Application.Features.Prompts.Commands.UpdatePromptStatus;

public sealed class UpdatePromptStatusValidator : AbstractValidator<UpdatePromptStatusCommand>
{
    public UpdatePromptStatusValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.RowVersion).NotEmpty();
    }
}
