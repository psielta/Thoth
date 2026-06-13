using FluentValidation;

namespace Thoth.Application.Features.Terminals.Commands.CloseTerminalSession;

public sealed class CloseTerminalSessionValidator : AbstractValidator<CloseTerminalSessionCommand>
{
    public CloseTerminalSessionValidator()
    {
        RuleFor(command => command.SessionId).NotEmpty();
    }
}