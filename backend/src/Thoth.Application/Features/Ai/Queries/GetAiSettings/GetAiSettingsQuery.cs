using MediatR;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Queries.GetAiSettings;

public sealed record GetAiSettingsQuery : IRequest<AiSettingsDto>;
