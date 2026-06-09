using FluentValidation;

namespace Thoth.Application.Features.Workflow.Commands.AddReviewVerdict;

public sealed class AddReviewVerdictValidator : AbstractValidator<AddReviewVerdictCommand>
{
    public AddReviewVerdictValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.Verdict).NotEmpty();
        RuleFor(command => command.RowVersion).NotEmpty();
    }
}
