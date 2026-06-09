using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Prompts.Queries.GetPrompt;

public sealed record GetPromptQuery(Guid Id) : IRequest<PromptDto>;
