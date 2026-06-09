using FluentValidation;

namespace Thoth.Application.Features.Ai.Commands.RefinePrompt;

public sealed class RefinePromptValidator : AbstractValidator<RefinePromptCommand>
{
    public RefinePromptValidator()
    {
        RuleFor(c => c.Content).NotEmpty().MaximumLength(200_000);
        RuleFor(c => c.Model).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Temperature).InclusiveBetween(0.0, 2.0);
        RuleFor(c => c.ContextFiles).NotNull();
        RuleFor(c => c.ContextFiles)
            .Must(files => files.Count <= 20)
            .When(c => c.ContextFiles is not null)
            .WithMessage("Selecione no maximo 20 arquivos de contexto.");
        RuleForEach(c => c.ContextFiles)
            .NotEmpty()
            .MaximumLength(1024)
            .Must(BeSafeRelativePath)
            .When(c => c.ContextFiles is not null)
            .WithMessage("Caminho invalido ou fora do diretorio de trabalho.");
        RuleFor(c => c.WorkingDirectoryId)
            .NotNull()
            .When(c => c.ContextFiles is { Count: > 0 })
            .WithMessage("Selecione um workspace para usar arquivos de contexto.");
        RuleFor(c => c.CustomInstructions)
            .MaximumLength(20_000)
            .When(c => c.CustomInstructions is not null);
    }

    private static bool BeSafeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            return false;
        }

        return !path.Split('/', '\\').Any(segment => segment == "..");
    }
}
