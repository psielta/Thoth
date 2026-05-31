using MediatR;

namespace PromptTasks.Application.Features.Ai.Commands.ReleasePromptAiSessions;

public sealed record ReleasePromptAiSessionsCommand(Guid PromptId) : IRequest;
