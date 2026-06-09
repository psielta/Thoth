using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.AgentUsage.Queries.GetAgentUsage;

public sealed record GetAgentUsageQuery : IRequest<AgentUsageDto>;
