using MediatR;

namespace PromptTasks.Application.Features.Ai.Commands.DeleteChatSession;

public sealed record DeleteChatSessionCommand(Guid SessionId) : IRequest;
