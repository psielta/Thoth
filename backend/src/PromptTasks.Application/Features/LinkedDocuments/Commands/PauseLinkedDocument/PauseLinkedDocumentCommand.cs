using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.PauseLinkedDocument;

public sealed record PauseLinkedDocumentCommand(Guid Id) : IRequest<LinkedDocumentDto>;
