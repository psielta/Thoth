using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Terminals.Queries.ListGenericTerminalSessions;

public sealed record ListGenericTerminalSessionsQuery : IRequest<IReadOnlyList<TerminalSessionDescriptor>>;
