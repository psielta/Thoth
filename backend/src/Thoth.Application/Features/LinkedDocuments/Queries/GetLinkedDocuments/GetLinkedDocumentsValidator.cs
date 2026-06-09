using FluentValidation;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocuments;

public sealed class GetLinkedDocumentsValidator : AbstractValidator<GetLinkedDocumentsQuery>
{
    public GetLinkedDocumentsValidator()
    {
        RuleFor(query => query.PromptId).NotEmpty();
    }
}
