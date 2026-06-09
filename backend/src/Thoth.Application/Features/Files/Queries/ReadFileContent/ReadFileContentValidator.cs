using FluentValidation;

namespace Thoth.Application.Features.Files.Queries.ReadFileContent;

public sealed class ReadFileContentValidator : AbstractValidator<ReadFileContentQuery>
{
    public ReadFileContentValidator()
    {
        RuleFor(query => query.WorkingDirectoryId).NotEmpty();
        RuleFor(query => query.RelativePath).NotEmpty().MaximumLength(1024);
    }
}