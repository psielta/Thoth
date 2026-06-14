using FluentValidation;

namespace Thoth.Application.Features.Files.Commands.OpenFileInVsCode;

public sealed class OpenFileInVsCodeValidator : AbstractValidator<OpenFileInVsCodeCommand>
{
    public OpenFileInVsCodeValidator()
    {
        RuleFor(command => command.WorkingDirectoryId).NotEmpty();
        RuleFor(command => command.RelativePath).NotEmpty().MaximumLength(1024);
    }
}
