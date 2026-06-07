using FluentValidation;

namespace PromptTasks.Application.Features.Workflow.Commands.AddReviewVerdict;

public sealed class AddReviewVerdictValidator : AbstractValidator<AddReviewVerdictCommand>
{
    public AddReviewVerdictValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.Verdict).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.RowVersion).NotEmpty();
    }
}
