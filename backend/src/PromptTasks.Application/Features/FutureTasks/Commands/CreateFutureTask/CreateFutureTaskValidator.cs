using FluentValidation;

namespace PromptTasks.Application.Features.FutureTasks.Commands.CreateFutureTask;

public sealed class CreateFutureTaskValidator : AbstractValidator<CreateFutureTaskCommand>
{
    public CreateFutureTaskValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(220);
        RuleFor(command => command.Description).MaximumLength(20000);
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.IssueGithubId).MaximumLength(64);
        RuleForEach(command => command.Labels)
            .Must(FutureTaskLabels.IsAllowed)
            .WithMessage("Label '{PropertyValue}' is not allowed.");
    }
}
