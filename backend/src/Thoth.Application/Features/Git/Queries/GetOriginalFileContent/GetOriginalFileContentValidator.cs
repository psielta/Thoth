using FluentValidation;

namespace Thoth.Application.Features.Git.Queries.GetOriginalFileContent;

public sealed class GetOriginalFileContentValidator : AbstractValidator<GetOriginalFileContentQuery>
{
    public GetOriginalFileContentValidator()
    {
        RuleFor(query => query.WorkingDirectoryId).NotEmpty();
        RuleFor(query => query.Path).NotEmpty().MaximumLength(1024);
    }
}
