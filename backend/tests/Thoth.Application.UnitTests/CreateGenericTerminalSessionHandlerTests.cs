using FluentAssertions;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Terminals;
using Thoth.Application.Features.Terminals.Commands.CreateGenericTerminalSession;
using Thoth.Domain.Users;

namespace Thoth.Application.UnitTests;

public sealed class CreateGenericTerminalSessionHandlerTests
{
    [Fact]
    public async Task Handle_creates_generic_session_for_current_user()
    {
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(new FakeCurrentUser(), coordinator);

        var result = await handler.Handle(
            new CreateGenericTerminalSessionCommand("pwsh.exe", null),
            CancellationToken.None);

        coordinator.LastCreate.Should().NotBeNull();
        coordinator.LastCreate!.Value.OwnerId.Should().Be(User.SystemUserId);
        coordinator.LastCreate.Value.Cwd.Should().BeNull();
        coordinator.LastCreate.Value.Shell.Should().Be("pwsh.exe");
        coordinator.LastCreate.Value.InitialInput.Should().BeNull();
        result.PromptId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_forwards_agent_launch_as_initial_input()
    {
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(new FakeCurrentUser(), coordinator);

        await handler.Handle(
            new CreateGenericTerminalSessionCommand(null, TerminalAgentLaunch.Codex),
            CancellationToken.None);

        coordinator.LastCreate!.Value.InitialInput.Should().BeEquivalentTo("codex --yolo\r"u8.ToArray());
    }

    [Fact]
    public async Task Handle_rejects_claude_plan_launch()
    {
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(new FakeCurrentUser(), coordinator);

        var act = () => handler.Handle(
            new CreateGenericTerminalSessionCommand(null, TerminalAgentLaunch.ClaudePlan),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        coordinator.LastCreate.Should().BeNull();
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => User.SystemUserId;
    }

    private sealed class RecordingTerminalCoordinator : ITerminalSessionCoordinator
    {
        public (Guid OwnerId, string? Cwd, string Shell, byte[]? InitialInput, byte[]? FollowUpInput)? LastCreate
        {
            get;
            private set;
        }

        public Task<TerminalSessionDescriptor> CreateGenericAsync(
            Guid ownerId,
            string? cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null)
        {
            LastCreate = (ownerId, cwd, shell, initialInput, followUpInput);
            return Task.FromResult(new TerminalSessionDescriptor(
                Guid.CreateVersion7(),
                null,
                shell,
                cwd ?? "C:/Users/dev",
                DateTimeOffset.UtcNow));
        }

        public Task<TerminalSessionDescriptor> CreateAsync(
            Guid promptId,
            string cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null) =>
            throw new NotSupportedException();

        public void WriteInput(Guid sessionId, byte[] input)
        {
        }

        public void Resize(Guid sessionId, ushort cols, ushort rows)
        {
        }

        public Task CloseAsync(Guid sessionId, CancellationToken cancellationToken) => Task.CompletedTask;

        public void AttachConnection(Guid sessionId, string connectionId)
        {
        }

        public void DetachConnection(Guid sessionId, string connectionId)
        {
        }

        public void ReleaseConnection(string connectionId)
        {
        }

        public IReadOnlyList<TerminalSessionDescriptor> ListForPrompt(Guid promptId) =>
            Array.Empty<TerminalSessionDescriptor>();

        public IReadOnlyList<TerminalSessionDescriptor> ListForOwner(Guid ownerId) =>
            Array.Empty<TerminalSessionDescriptor>();

        public IReadOnlyList<TerminalSessionDescriptor> ListAll() =>
            Array.Empty<TerminalSessionDescriptor>();

        public TerminalSessionDescriptor? TryGetSession(Guid sessionId) => null;

        public Task KillForPromptAsync(Guid promptId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
