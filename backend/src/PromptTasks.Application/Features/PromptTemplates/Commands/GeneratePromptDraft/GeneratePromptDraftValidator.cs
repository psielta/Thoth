using FluentValidation;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.PromptTemplates.Commands.GeneratePromptDraft;

public sealed class GeneratePromptDraftValidator : AbstractValidator<GeneratePromptDraftCommand>
{
    public GeneratePromptDraftValidator()
    {
        RuleFor(command => command.LinkedDocumentId).NotEmpty();
        RuleFor(command => command.TemplateKey).IsInEnum();
        When(
            command => command.TemplateKey is PromptTemplateKey.ReviewPullRequest or PromptTemplateKey.MergePullRequest,
            () =>
            {
                RuleFor(command => command.PullRequest)
                    .NotEmpty()
                    .MaximumLength(120);
            });
    }
}
