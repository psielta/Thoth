using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Terminals;

namespace Thoth.Application.Features.Terminals.Commands.CreateGenericTerminalSession;

public sealed record CreateGenericTerminalSessionCommand(
    string? Shell,
    TerminalAgentLaunch? AgentLaunch) : IRequest<TerminalSessionDescriptor>;
