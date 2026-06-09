using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.PromptTemplates.Queries.GetPromptTemplates;

public sealed record GetPromptTemplatesQuery : IRequest<IReadOnlyList<PromptTemplateDto>>;
