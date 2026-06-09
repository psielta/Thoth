using FluentValidation;

namespace Thoth.Application.Features.LinkedDocuments.Commands.LinkDocument;

public sealed class LinkDocumentValidator : AbstractValidator<LinkDocumentCommand>
{
    public LinkDocumentValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.AbsolutePath)
            .NotEmpty()
            .MaximumLength(1024)
            .Must(Path.IsPathFullyQualified)
            .WithMessage("Path must be absolute.")
            .Must(LinkedDocumentHelpers.HasMarkdownExtension)
            .WithMessage("Only .md and .markdown files can be linked.");
        RuleFor(command => command.DisplayName).MaximumLength(260);
        RuleFor(command => command.DocumentType).IsInEnum();
    }
}
