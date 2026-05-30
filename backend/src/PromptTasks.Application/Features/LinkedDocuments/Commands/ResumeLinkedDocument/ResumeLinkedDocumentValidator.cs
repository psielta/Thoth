using FluentValidation;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.ResumeLinkedDocument;

public sealed class ResumeLinkedDocumentValidator : AbstractValidator<ResumeLinkedDocumentCommand>
{
    public ResumeLinkedDocumentValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
