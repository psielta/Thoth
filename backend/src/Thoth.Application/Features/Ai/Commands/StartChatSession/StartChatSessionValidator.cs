using FluentValidation;

namespace Thoth.Application.Features.Ai.Commands.StartChatSession;

public sealed class StartChatSessionValidator : AbstractValidator<StartChatSessionCommand>
{
    public StartChatSessionValidator()
    {
        RuleFor(c => c.Model).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Temperature).InclusiveBetween(0.0, 2.0);
        RuleFor(c => c.Title).MaximumLength(220).When(c => c.Title is not null);
    }
}
