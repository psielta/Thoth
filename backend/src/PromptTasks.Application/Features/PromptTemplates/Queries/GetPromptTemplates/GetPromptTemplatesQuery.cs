using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.PromptTemplates.Queries.GetPromptTemplates;

public sealed record GetPromptTemplatesQuery : IRequest<IReadOnlyList<PromptTemplateDto>>;
