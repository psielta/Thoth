using MediatR;

namespace PromptTasks.Application.Features.WorkingDirectories.Commands.DeleteWorkingDirectory;

public sealed record DeleteWorkingDirectoryCommand(Guid Id) : IRequest;
