using MediatR;

namespace Thoth.Application.Features.Ai.Commands.DeleteChatSession;

public sealed record DeleteChatSessionCommand(Guid SessionId) : IRequest;
