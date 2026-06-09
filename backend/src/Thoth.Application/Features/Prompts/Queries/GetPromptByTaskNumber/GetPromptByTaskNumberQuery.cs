using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Prompts.Queries.GetPromptByTaskNumber;

public sealed record GetPromptByTaskNumberQuery(Guid WorkingDirectoryId, string TaskNumber) : IRequest<PromptDto>;
