using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.AddWorkflowNote;

public sealed record AddWorkflowNoteCommand(Guid PromptId, string Note) : IRequest<WorkflowDto>;
