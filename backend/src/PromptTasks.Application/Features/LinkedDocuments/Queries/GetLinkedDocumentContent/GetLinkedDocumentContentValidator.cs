using FluentValidation;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentContent;

public sealed class GetLinkedDocumentContentValidator : AbstractValidator<GetLinkedDocumentContentQuery>
{
    public GetLinkedDocumentContentValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
        RuleFor(query => query.VersionNumber).GreaterThan(0).When(query => query.VersionNumber.HasValue);
    }
}
