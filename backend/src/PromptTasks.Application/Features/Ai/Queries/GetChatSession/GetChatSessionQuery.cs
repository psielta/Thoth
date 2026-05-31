using MediatR;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Queries.GetChatSession;

public sealed record GetChatSessionQuery(Guid Id) : IRequest<AiChatSessionDto>;
