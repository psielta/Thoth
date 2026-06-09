using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Prompts.Queries.GetPromptByTaskNumber;

public sealed class GetPromptByTaskNumberHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetPromptByTaskNumberQuery, PromptDto>
{
    public Task<PromptDto> Handle(GetPromptByTaskNumberQuery request, CancellationToken cancellationToken)
    {
        var prompt = context.Prompts.FirstOrDefault(item =>
            item.OwnerId == currentUser.UserId &&
            item.WorkingDirectoryId == request.WorkingDirectoryId &&
            item.ParentPromptId == null &&
            item.TaskNumber == request.TaskNumber);

        if (prompt is null)
        {
            throw new NotFoundException("Prompt was not found.");
        }

        var references = context.PromptFileReferences
            .Where(reference => reference.PromptId == prompt.Id)
            .ToList();

        return Task.FromResult(prompt.ToDto(references));
    }
}
