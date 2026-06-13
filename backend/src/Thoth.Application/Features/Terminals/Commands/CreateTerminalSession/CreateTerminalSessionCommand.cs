using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Terminals.Commands.CreateTerminalSession;

public sealed record CreateTerminalSessionCommand(Guid PromptId, string? Shell) : IRequest<TerminalSessionDescriptor>;