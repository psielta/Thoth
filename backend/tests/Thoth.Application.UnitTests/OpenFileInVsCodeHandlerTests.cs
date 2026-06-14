using FluentAssertions;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Files.Commands.OpenFileInVsCode;

namespace Thoth.Application.UnitTests;

public sealed class OpenFileInVsCodeHandlerTests
{
    [Fact]
    public async Task Handle_opens_the_resolved_file_in_the_owned_workspace()
    {
        var context = new InMemoryAiDbContext();
        var workspace = context.SeedWorkspace(enableAiContext: false);
        var launcher = new RecordingEditorLauncher();
        var handler = new OpenFileInVsCodeHandler(
            context,
            new StubCurrentUser(),
            new StubWorkspaceFileService(),
            launcher);

        await handler.Handle(
            new OpenFileInVsCodeCommand(workspace.Id, "src/Program.cs"),
            CancellationToken.None);

        launcher.OpenedWorkspacePath.Should().Be(workspace.AbsolutePath);
        launcher.OpenedFilePath.Should().Be(Path.Combine(workspace.AbsolutePath, "src/Program.cs"));
    }

    [Fact]
    public async Task Handle_rejects_workspaces_from_other_users()
    {
        var context = new InMemoryAiDbContext();
        var workspace = context.SeedWorkspace(enableAiContext: false, ownerId: Guid.CreateVersion7());
        var launcher = new RecordingEditorLauncher();
        var handler = new OpenFileInVsCodeHandler(
            context,
            new StubCurrentUser(),
            new StubWorkspaceFileService(),
            launcher);

        var act = () => handler.Handle(
            new OpenFileInVsCodeCommand(workspace.Id, "src/Program.cs"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        launcher.OpenedFilePath.Should().BeNull();
    }

    private sealed class RecordingEditorLauncher : IWorkspaceEditorLauncher
    {
        public string? OpenedWorkspacePath { get; private set; }
        public string? OpenedFilePath { get; private set; }

        public Task OpenVsCodeAsync(string absolutePath, CancellationToken cancellationToken)
        {
            OpenedWorkspacePath = absolutePath;
            return Task.CompletedTask;
        }

        public Task OpenFileInVsCodeAsync(string workspaceAbsolutePath, string fileAbsolutePath, CancellationToken cancellationToken)
        {
            OpenedWorkspacePath = workspaceAbsolutePath;
            OpenedFilePath = fileAbsolutePath;
            return Task.CompletedTask;
        }
    }
}
