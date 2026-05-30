using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Prompts;

namespace PromptTasks.Application.Features.Prompts.Queries.GetPrompt;

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
