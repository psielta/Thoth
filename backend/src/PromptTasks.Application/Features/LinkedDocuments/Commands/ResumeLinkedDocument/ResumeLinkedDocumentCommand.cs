using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.ResumeLinkedDocument;

public sealed record ResumeLinkedDocumentCommand(Guid Id) : IRequest<LinkedDocumentDto>;
