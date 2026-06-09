using MediatR;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Queries.GetChatSession;

public sealed record GetChatSessionQuery(Guid Id) : IRequest<AiChatSessionDto>;
