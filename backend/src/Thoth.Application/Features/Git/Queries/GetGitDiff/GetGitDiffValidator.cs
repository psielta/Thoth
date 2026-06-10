using FluentValidation;

namespace Thoth.Application.Features.Git.Queries.GetGitDiff;

public sealed class GetGitDiffValidator : AbstractValidator<GetGitDiffQuery>
{
    public GetGitDiffValidator()
    {
        RuleFor(query => query.WorkingDirectoryId).NotEmpty();
        RuleFor(query => query.Path).NotEmpty().MaximumLength(1024);
    }
}
