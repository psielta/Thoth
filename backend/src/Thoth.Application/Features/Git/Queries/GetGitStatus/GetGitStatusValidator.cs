using FluentValidation;

namespace Thoth.Application.Features.Git.Queries.GetGitStatus;

public sealed class GetGitStatusValidator : AbstractValidator<GetGitStatusQuery>
{
    public GetGitStatusValidator()
    {
        RuleFor(query => query.WorkingDirectoryId).NotEmpty();
    }
}
