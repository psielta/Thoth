using FluentValidation;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.RemoveLinkedDocument;

public sealed class RemoveLinkedDocumentValidator : AbstractValidator<RemoveLinkedDocumentCommand>
{
    public RemoveLinkedDocumentValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
