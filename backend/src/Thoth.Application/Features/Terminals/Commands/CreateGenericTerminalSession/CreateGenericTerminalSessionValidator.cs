using FluentValidation;

namespace Thoth.Application.Features.Terminals.Commands.CreateGenericTerminalSession;

public sealed class CreateGenericTerminalSessionValidator : AbstractValidator<CreateGenericTerminalSessionCommand>
{
    public CreateGenericTerminalSessionValidator()
    {
        RuleFor(command => command.Shell)
            .MaximumLength(260)
            .When(command => !string.IsNullOrWhiteSpace(command.Shell));
        RuleFor(command => command.AgentLaunch)
            .IsInEnum()
            .When(command => command.AgentLaunch.HasValue);
    }
}
