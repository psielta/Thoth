using FluentValidation;

namespace Thoth.Application.Features.FutureTasks.Commands.UpdateFutureTaskStatus;

public sealed class UpdateFutureTaskStatusValidator : AbstractValidator<UpdateFutureTaskStatusCommand>
{
    public UpdateFutureTaskStatusValidator()
    {
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.RowVersion).NotEmpty();
    }
}
