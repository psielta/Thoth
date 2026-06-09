using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;

namespace Thoth.Application.Features.Prompts.Queries.GetPrompt;

public sealed class GetPromptHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetPromptQuery, PromptDto>
{
    public Task<PromptDto> Handle(GetPromptQuery request, CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.Id, currentUser.UserId);
        var references = context.PromptFileReferences
            .Where(reference => reference.PromptId == prompt.Id)
            .ToList();

        return Task.FromResult(prompt.ToDto(references));
    }
}
