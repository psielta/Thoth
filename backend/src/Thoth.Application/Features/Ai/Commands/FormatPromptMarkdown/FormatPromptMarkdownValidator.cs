using FluentValidation;

namespace Thoth.Application.Features.Ai.Commands.FormatPromptMarkdown;

public sealed class FormatPromptMarkdownValidator : AbstractValidator<FormatPromptMarkdownCommand>
{
    public FormatPromptMarkdownValidator()
    {
        RuleFor(c => c.Content).NotEmpty().MaximumLength(200_000);
        RuleFor(c => c.Model).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Temperature).InclusiveBetween(0.0, 2.0);
    }
}