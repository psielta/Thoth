using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.AgentUsage.Queries.GetAgentUsage;

public sealed class GetAgentUsageHandler(IAgentUsageReader reader) : IRequestHandler<GetAgentUsageQuery, AgentUsageDto>
{
    public Task<AgentUsageDto> Handle(GetAgentUsageQuery request, CancellationToken cancellationToken) =>
        reader.ReadAsync(cancellationToken);
}
