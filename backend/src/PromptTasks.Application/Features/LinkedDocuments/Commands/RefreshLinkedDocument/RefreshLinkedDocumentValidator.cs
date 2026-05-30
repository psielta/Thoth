using FluentValidation;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.RefreshLinkedDocument;

public sealed class RefreshLinkedDocumentValidator : AbstractValidator<RefreshLinkedDocumentCommand>
{
    public RefreshLinkedDocumentValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
