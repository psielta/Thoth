using FluentAssertions;
using FluentValidation;
using PromptTasks.Application.Common.Behaviors;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Prompts.Commands.CreatePrompt;
using PromptTasks.Domain.Prompts;
using PromptTasks.Domain.Users;
using PromptTasks.Domain.WorkingDirectories;

namespace PromptTasks.Application.UnitTests;

public sealed class CreatePromptHandlerTests
{
    [Fact]
    public async Task Handle_creates_prompt_version_references_and_notifies()
    {
        var context = new FakeApplicationDbContext();
        context.WorkingDirectoryItems.Add(new WorkingDirectory
        {
            Id = Guid.CreateVersion7(),
            Name = "repo",
            AbsolutePath = "C:/repo",
            OwnerId = User.SystemUserId
        });
        var notifier = new FakePromptNotifier();

        var handler = new CreatePromptHandler(
            context,
            new FakeWorkspaceFileService(),
            notifier,
            new FakeCurrentUser(),
            new FakeDateTimeProvider());

        var command = new CreatePromptCommand(
            context.WorkingDirectoryItems[0].Id,
            "Fix main",
            "Please inspect @src/main.go",
            TargetAgent.Codex,
            PromptKind.General,
            PromptStatus.Draft,
            new[] { new FileMentionDto("src/main.go", "src/main.go") });

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Fix main");
        context.PromptItems.Should().ContainSingle();
        context.PromptVersionItems.Should().ContainSingle(version => version.VersionNumber == 1);
        context.PromptFileReferenceItems.Should().ContainSingle(reference => reference.RelativePath == "src/main.go");
        notifier.Created.Should().Be(result);
        context.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidationBehavior_aggregates_validation_failures()
    {
        var behavior = new ValidationBehavior<CreatePromptCommand, PromptDto>(new[] { new CreatePromptValidator() });
        var invalid = new CreatePromptCommand(Guid.Empty, "", "", (TargetAgent)999, PromptKind.General, PromptStatus.Draft, null);

        var act = () => behavior.Handle(
            invalid,
            _ => Task.FromResult(new PromptDto(
                Guid.Empty,
                Guid.Empty,
                "",
                "",
                TargetAgent.Codex,
                PromptKind.General,
                PromptStatus.Draft,
                1,
                "0",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                Array.Empty<FileMentionDto>())),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private sealed class FakeApplicationDbContext : IApplicationDbContext
    {
        public List<User> UserItems { get; } = new();
        public List<WorkingDirectory> WorkingDirectoryItems { get; } = new();
        public List<Prompt> PromptItems { get; } = new();
        public List<PromptVersion> PromptVersionItems { get; } = new();
        public List<PromptFileReference> PromptFileReferenceItems { get; } = new();
        public List<LinkedDocument> LinkedDocumentItems { get; } = new();
        public int SaveChangesCount { get; private set; }

        public IQueryable<User> Users => UserItems.AsQueryable();
        public IQueryable<WorkingDirectory> WorkingDirectories => WorkingDirectoryItems.AsQueryable();
        public IQueryable<Prompt> Prompts => PromptItems.AsQueryable();
        public IQueryable<PromptVersion> PromptVersions => PromptVersionItems.AsQueryable();
        public IQueryable<PromptFileReference> PromptFileReferences => PromptFileReferenceItems.AsQueryable();
        public IQueryable<LinkedDocument> LinkedDocuments => LinkedDocumentItems.AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            AddToList(entity);
        }

        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities)
            {
                AddToList(entity);
            }
        }

        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            switch (entity)
            {
                case Prompt prompt:
                    PromptItems.Remove(prompt);
                    break;
                case PromptFileReference reference:
                    PromptFileReferenceItems.Remove(reference);
                    break;
            }
        }

        public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities.ToList())
            {
                Remove(entity);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }

        private void AddToList<TEntity>(TEntity entity) where TEntity : class
        {
            switch (entity)
            {
                case Prompt prompt:
                    PromptItems.Add(prompt);
                    break;
                case PromptVersion version:
                    PromptVersionItems.Add(version);
                    break;
                case PromptFileReference reference:
                    PromptFileReferenceItems.Add(reference);
                    break;
                case WorkingDirectory directory:
                    WorkingDirectoryItems.Add(directory);
                    break;
            }
        }
    }

    private sealed class FakeWorkspaceFileService : IWorkspaceFileService
    {
        public Task<ValidatedPathResult> ValidatePathAsync(string absolutePath, CancellationToken cancellationToken) =>
            Task.FromResult(ValidatedPathResult.Valid(absolutePath));

        public Task<IReadOnlyList<FileSearchResultDto>> SearchAsync(
            Guid workingDirectoryId,
            string rootAbsolutePath,
            string query,
            int limit,
            bool respectGitignore,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FileSearchResultDto>>(Array.Empty<FileSearchResultDto>());

        public Task<FileReferenceResolution> ResolveRelativePathAsync(
            string rootAbsolutePath,
            string relativePath,
            CancellationToken cancellationToken) =>
            Task.FromResult(new FileReferenceResolution(relativePath, true, DateTimeOffset.UtcNow));
    }

    private sealed class FakePromptNotifier : IPromptNotifier
    {
        public PromptDto? Created { get; private set; }

        public Task PromptCreatedAsync(PromptDto prompt, CancellationToken cancellationToken)
        {
            Created = prompt;
            return Task.CompletedTask;
        }

        public Task PromptUpdatedAsync(PromptDto prompt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PromptDeletedAsync(Guid promptId, Guid workingDirectoryId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => User.SystemUserId;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    }
}
