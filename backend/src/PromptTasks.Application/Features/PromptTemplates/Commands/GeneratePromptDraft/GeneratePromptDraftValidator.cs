using FluentValidation;

namespace PromptTasks.Application.Features.PromptTemplates.Commands.GeneratePromptDraft;

public sealed class GeneratePromptDraftValidator : AbstractValidator<GeneratePromptDraftCommand>
{
    public GeneratePromptDraftValidator()
    {
        RuleFor(command => command.LinkedDocumentId).NotEmpty();
        RuleFor(command => command.TemplateKey).IsInEnum();
    }
}
