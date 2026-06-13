using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Terminals.Queries.ListTerminalSessions;

public sealed record ListTerminalSessionsQuery(Guid PromptId) : IRequest<IReadOnlyList<TerminalSessionDescriptor>>;