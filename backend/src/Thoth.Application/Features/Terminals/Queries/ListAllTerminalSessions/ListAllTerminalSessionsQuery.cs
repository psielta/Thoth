using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Terminals.Queries.ListAllTerminalSessions;

public sealed record ListAllTerminalSessionsQuery : IRequest<IReadOnlyList<PromptTerminalsGroupDto>>;
