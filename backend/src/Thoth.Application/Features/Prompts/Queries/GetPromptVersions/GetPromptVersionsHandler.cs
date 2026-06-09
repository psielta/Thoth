using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;

namespace Thoth.Application.Features.Prompts.Queries.GetPromptVersions;

public sealed class GetPromptVersionsHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetPromptVersionsQuery, IReadOnlyList<PromptVersionDto>>
{
    public Task<IReadOnlyList<PromptVersionDto>> Handle(GetPromptVersionsQuery request, CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.PromptId, currentUser.UserId);

        IReadOnlyList<PromptVersionDto> result = context.PromptVersions
            .Where(version => version.PromptId == prompt.Id)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => version.ToDto())
            .ToList();

        return Task.FromResult(result);
    }
}
