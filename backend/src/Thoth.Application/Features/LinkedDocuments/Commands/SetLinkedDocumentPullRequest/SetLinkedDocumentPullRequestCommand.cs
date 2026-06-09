using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Commands.SetLinkedDocumentPullRequest;

public sealed record SetLinkedDocumentPullRequestCommand(Guid Id, string? PullRequest) : IRequest<LinkedDocumentDto>;
