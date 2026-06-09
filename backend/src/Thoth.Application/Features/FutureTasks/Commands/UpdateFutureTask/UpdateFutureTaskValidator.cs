using FluentValidation;

namespace Thoth.Application.Features.FutureTasks.Commands.UpdateFutureTask;

public sealed class UpdateFutureTaskValidator : AbstractValidator<UpdateFutureTaskCommand>
{
    public UpdateFutureTaskValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(220);
        RuleFor(command => command.Description).MaximumLength(20000);
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.IssueGithubId).MaximumLength(64);
        RuleFor(command => command.RowVersion).NotEmpty();
        RuleForEach(command => command.Labels)
            .Must(FutureTasks.FutureTaskLabels.IsAllowed)
            .WithMessage("Label '{PropertyValue}' is not allowed.");
    }
}
