using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.AddWorkflowNote;

public sealed record AddWorkflowNoteCommand(Guid PromptId, string Note) : IRequest<WorkflowDto>;
