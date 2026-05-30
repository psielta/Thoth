using FluentValidation;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.PauseLinkedDocument;

public sealed class PauseLinkedDocumentValidator : AbstractValidator<PauseLinkedDocumentCommand>
{
    public PauseLinkedDocumentValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
