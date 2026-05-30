using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Prompts.Queries.GetPrompt;

public sealed record GetPromptQuery(Guid Id) : IRequest<PromptDto>;
