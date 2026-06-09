using MediatR;

namespace Thoth.Application.Features.Ai.Commands.ReleasePromptAiSessions;

public sealed record ReleasePromptAiSessionsCommand(Guid PromptId) : IRequest;
