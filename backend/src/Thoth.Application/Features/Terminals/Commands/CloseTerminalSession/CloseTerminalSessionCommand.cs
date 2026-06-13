using MediatR;

namespace Thoth.Application.Features.Terminals.Commands.CloseTerminalSession;

public sealed record CloseTerminalSessionCommand(Guid SessionId) : IRequest;