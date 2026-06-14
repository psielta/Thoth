using FluentAssertions;
using FluentValidation;
using Thoth.Application.Common.Behaviors;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.PromptTemplates;
using Thoth.Application.Features.PromptTemplates.Commands.GeneratePromptDraft;
using Thoth.Application.Features.PromptTemplates.Definitions;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;
using Thoth.Domain.Workflows;

namespace Thoth.Application.UnitTests;

public sealed class PromptTemplateHandlerTests
{
    [Fact]
    public void Catalog_returns_templates_ordered_by_key()
    {
        var catalog = CreateCatalog();

        var templates = catalog.GetAll();

        templates.Select(template => template.Key).Should()
            .Equal(
                PromptTemplateKey.ReviewPlan,
                PromptTemplateKey.ImplementPlan,
                PromptTemplateKey.ReviewPlanWithParentPrompt,
                PromptTemplateKey.ReReviewPlan,
                PromptTemplateKey.ImplementPlanInWorktree,
                PromptTemplateKey.ReviewPullRequest,
                PromptTemplateKey.ReReviewPullRequest,
                PromptTemplateKey.RebaseCurrentBranch,
                PromptTemplateKey.MergePullRequest);
        catalog.Get(PromptTemplateKey.ReviewPlan).Should().BeOfType<ReviewPlanTemplate>();
        catalog.Get(PromptTemplateKey.ImplementPlan).Should().BeOfType<ImplementPlanTemplate>();
        catalog.Get(PromptTemplateKey.ReviewPlanWithParentPrompt).Should().BeOfType<ReviewPlanWithParentPromptTemplate>();
        catalog.Get(PromptTemplateKey.ReReviewPlan).Should().BeOfType<ReReviewPlanTemplate>();
        catalog.Get(PromptTemplateKey.ImplementPlanInWorktree).Should().BeOfType<ImplementPlanInWorktreeTemplate>();
        catalog.Get(PromptTemplateKey.ReviewPullRequest).Should().BeOfType<ReviewPullRequestTemplate>();
        catalog.Get(PromptTemplateKey.ReReviewPullRequest).Should().BeOfType<ReReviewPullRequestTemplate>();
        catalog.Get(PromptTemplateKey.MergePullRequest).Should().BeOfType<MergePullRequestTemplate>();
        catalog.Get(PromptTemplateKey.RebaseCurrentBranch).Should().BeOfType<RebaseCurrentBranchTemplate>();
    }

    [Theory]
    [InlineData(PromptTemplateKey.ReviewPlan, WorkflowPhaseRole.PlanReview, false)]
    [InlineData(PromptTemplateKey.ReviewPlanWithParentPrompt, WorkflowPhaseRole.PlanReview, false)]
    [InlineData(PromptTemplateKey.ReReviewPlan, WorkflowPhaseRole.PlanReview, true)]
    [InlineData(PromptTemplateKey.ImplementPlan, WorkflowPhaseRole.Implementation, false)]
    [InlineData(PromptTemplateKey.ImplementPlanInWorktree, WorkflowPhaseRole.Implementation, false)]
    [InlineData(PromptTemplateKey.ReviewPullRequest, WorkflowPhaseRole.CodeReview, false)]
    [InlineData(PromptTemplateKey.ReReviewPullRequest, WorkflowPhaseRole.CodeReview, true)]
    [InlineData(PromptTemplateKey.RebaseCurrentBranch, WorkflowPhaseRole.Rebase, false)]
    [InlineData(PromptTemplateKey.MergePullRequest, WorkflowPhaseRole.Merge, false)]
    public void Templates_expose_workflow_phase_metadata(
        PromptTemplateKey key,
        WorkflowPhaseRole targetRole,
        bool isReReview)
    {
        var template = CreateCatalog().Get(key);

        template.TargetPhaseRole.Should().Be(targetRole);
        template.IsReReview.Should().Be(isReReview);
    }

    [Fact]
    public void Catalog_throws_for_unknown_template_key()
    {
        var catalog = CreateCatalog();

        var act = () => catalog.Get((PromptTemplateKey)999);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public async Task GeneratePromptDraft_review_plan_uses_linked_document_path_and_parent_workspace()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/Users/psiel/.claude/plans/plan.md", "plan.md");
        context.LinkedDocumentVersionItems.Add(new LinkedDocumentVersion
        {
            LinkedDocumentId = document.Id,
            VersionNumber = 1,
            Content = "# Saved plan",
            ContentHash = "hash",
            SizeBytes = 12
        });
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ReviewPlan),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.ReviewPlan);
        result.LinkedDocumentId.Should().Be(document.Id);
        result.WorkingDirectoryId.Should().Be(prompt.WorkingDirectoryId);
        result.ParentPromptId.Should().Be(prompt.Id);
        result.Title.Should().Be("Revisar plano: plan.md");
        result.Content.Should().Be(
            "Dado o plano \"C:/Users/psiel/.claude/plans/plan.md\", valide o plano, aprove-o ou aponte melhorias.");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.Planning);
        context.SaveChangesCount.Should().Be(0);
    }

    [Fact]
    public async Task GeneratePromptDraft_implement_plan_uses_general_kind()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/implementation.md", "implementation.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ImplementPlan),
            CancellationToken.None);

        result.Title.Should().Be("Implementar plano: implementation.md");
        result.Content.Should().Be("Implemente o plano \"C:/plans/implementation.md\".");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.General);
    }

    [Fact]
    public async Task GeneratePromptDraft_review_plan_with_parent_prompt_includes_original_prompt()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId, "Faca um plano para @src/main.go");
        var document = SeedLinkedDocument(context, prompt, "C:/plans/plan.md", "plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ReviewPlanWithParentPrompt),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.ReviewPlanWithParentPrompt);
        result.Title.Should().Be("Revisar plano com prompt pai: plan.md");
        result.Content.Should().Be(
            """
            Pedi ao Claude para rodar o plan-mode usando o prompt abaixo:

            ```md
            Faca um plano para @src/main.go
            ```

            Ele gerou o plano "C:/plans/plan.md".

            Dado o plano "C:/plans/plan.md", valide o plano, aprove-o ou aponte melhorias.
            """);
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.Planning);
    }

    [Fact]
    public async Task GeneratePromptDraft_re_review_plan_explains_it_is_a_second_validation()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/reviewed-plan.md", "reviewed-plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ReReviewPlan),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.ReReviewPlan);
        result.Title.Should().Be("Revisar plano novamente: reviewed-plan.md");
        result.Content.Should().Be(
            "Passei os pontos anteriores para o Claude corrigir no plano \"C:/plans/reviewed-plan.md\". Valide o plano atualizado novamente, aprove-o se estiver correto ou aponte as melhorias que ainda faltam.");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.Planning);
    }

    [Fact]
    public async Task GeneratePromptDraft_implement_plan_in_worktree_requests_pr_creation()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/worktree-plan.md", "worktree-plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ImplementPlanInWorktree),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.ImplementPlanInWorktree);
        result.Title.Should().Be("Implementar em worktree: worktree-plan.md");
        result.Content.Should().Contain("Implemente o plano `C:/plans/worktree-plan.md` completamente em uma worktree separada.");
        result.Content.Should().Contain("abra um PR");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.General);
    }

    [Fact]
    public async Task GeneratePromptDraft_review_pull_request_uses_pr_reference()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/pr-plan.md", "pr-plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ReviewPullRequest, "123"),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.ReviewPullRequest);
        result.Title.Should().Be("Revisar PR #123: pr-plan.md");
        result.Content.Should().StartWith("/review");
        result.Content.Should().Contain("Revise o PR #123 que implementa o plano `C:/plans/pr-plan.md`.");
        result.Content.Should().Contain("Priorize bugs, riscos de comportamento e testes ausentes.");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.General);
    }

    [Fact]
    public async Task GeneratePromptDraft_merge_pull_request_uses_pr_reference()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/merge-plan.md", "merge-plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.MergePullRequest, "123"),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.MergePullRequest);
        result.Title.Should().Be("Mesclar PR #123: merge-plan.md");
        result.Content.Should().Contain("Faça o merge do PR #123 que implementa o plano `C:/plans/merge-plan.md`.");
        result.Content.Should().Contain("sincronize o branch main local com o remoto");
        result.Content.Should().Contain("remova a worktree se existir");
        result.Content.Should().Contain("exclua o branch local/remoto se ainda existirem e for seguro");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.General);
    }

    [Fact]
    public async Task GeneratePromptDraft_re_review_pull_request_uses_pr_reference_and_codex_response()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/pr-plan.md", "pr-plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(
                document.Id,
                PromptTemplateKey.ReReviewPullRequest,
                Inputs: new Dictionary<string, string>
                {
                    ["pullRequest"] = "123",
                    ["codexResponse"] = "Codex fixed the missing regression test."
                }),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.ReReviewPullRequest);
        result.Title.Should().Be("Revisar novamente PR #123: pr-plan.md");
        result.Content.Should().StartWith("/review");
        result.Content.Should().Contain("Revise novamente o PR #123 depois que o Codex corrigiu os pontos da revisão anterior.");
        result.Content.Should().Contain("O PR implementa o plano `C:/plans/pr-plan.md`.");
        result.Content.Should().Contain("Resposta do Codex após aplicar as correções:");
        result.Content.Should().Contain("Codex fixed the missing regression test.");
        result.Content.Should().Contain("Trate a resposta do Codex como um repasse, não como prova.");
        result.Content.Should().Contain("Se o PR estiver aceitável agora, diga isso claramente.");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.General);
    }

    [Fact]
    public async Task GeneratePromptDraft_rebase_current_branch_requests_rebase_from_remote_main()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/rebase-plan.md", "rebase-plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.RebaseCurrentBranch),
            CancellationToken.None);

        result.TemplateKey.Should().Be(PromptTemplateKey.RebaseCurrentBranch);
        result.Title.Should().Be("Atualizar branch com main: rebase-plan.md");
        result.Content.Should().Contain("Atualize meu branch/worktree atual com as últimas alterações do branch main remoto usando rebase.");
        result.Content.Should().Contain("Preserve as alterações locais não relacionadas.");
        result.Content.Should().Contain("Se houver conflitos, pare e me avise para resolvermos juntos.");
        result.TargetAgent.Should().Be(TargetAgent.Codex);
        result.Kind.Should().Be(PromptKind.General);
    }

    [Fact]
    public async Task GeneratePromptDraft_rejects_document_from_another_owner()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, Guid.CreateVersion7());
        var document = SeedLinkedDocument(context, prompt, "C:/plans/other.md", "other.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var act = () => handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ReviewPlan),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GeneratePromptDraft_validation_rejects_invalid_template_key()
    {
        var behavior = new ValidationBehavior<GeneratePromptDraftCommand, GeneratedPromptDraftDto>(
            new[] { new GeneratePromptDraftValidator() });
        var invalid = new GeneratePromptDraftCommand(Guid.CreateVersion7(), (PromptTemplateKey)999);

        var act = () => behavior.Handle(
            invalid,
            _ => Task.FromResult(new GeneratedPromptDraftDto(
                invalid.TemplateKey,
                invalid.LinkedDocumentId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "",
                "",
                TargetAgent.Codex,
                PromptKind.General)),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(PromptTemplateKey.ReviewPullRequest)]
    [InlineData(PromptTemplateKey.ReReviewPullRequest)]
    [InlineData(PromptTemplateKey.MergePullRequest)]
    public async Task GeneratePromptDraft_requires_pull_request_when_plan_has_none(PromptTemplateKey templateKey)
    {
        // A obrigatoriedade da PR foi movida para o handler (apos o fallback do plano vinculado).
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/pr-plan.md", "pr-plan.md");
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        var act = () => handler.Handle(
            new GeneratePromptDraftCommand(document.Id, templateKey),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GeneratePromptDraft_pulls_pull_request_from_linked_document()
    {
        var context = new FakeApplicationDbContext();
        var prompt = SeedPrompt(context, User.SystemUserId);
        var document = SeedLinkedDocument(context, prompt, "C:/plans/pr-plan.md", "pr-plan.md");
        document.PullRequestReference = "123";
        var handler = new GeneratePromptDraftHandler(context, CreateCatalog(), new FakeCurrentUser());

        // Sem PR na request: deve puxar a PR salva no plano vinculado.
        var result = await handler.Handle(
            new GeneratePromptDraftCommand(document.Id, PromptTemplateKey.ReviewPullRequest),
            CancellationToken.None);

        result.Title.Should().Be("Revisar PR #123: pr-plan.md");
        result.Content.Should().Contain("Revise o PR #123 que implementa o plano `C:/plans/pr-plan.md`.");
    }

    [Fact]
    public async Task GeneratePromptDraft_validation_requires_codex_response_for_re_review_pull_request_template()
    {
        var behavior = new ValidationBehavior<GeneratePromptDraftCommand, GeneratedPromptDraftDto>(
            new[] { new GeneratePromptDraftValidator() });
        var invalid = new GeneratePromptDraftCommand(Guid.CreateVersion7(), PromptTemplateKey.ReReviewPullRequest, "123");

        var act = () => behavior.Handle(
            invalid,
            _ => Task.FromResult(new GeneratedPromptDraftDto(
                invalid.TemplateKey,
                invalid.LinkedDocumentId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "",
                "",
                TargetAgent.Codex,
                PromptKind.General)),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static PromptTemplateCatalog CreateCatalog() =>
        new(new IPromptTemplateDefinition[]
        {
            new ImplementPlanTemplate(),
            new ReviewPlanTemplate(),
            new ReviewPlanWithParentPromptTemplate(),
            new ReReviewPlanTemplate(),
            new ImplementPlanInWorktreeTemplate(),
            new ReviewPullRequestTemplate(),
            new ReReviewPullRequestTemplate(),
            new MergePullRequestTemplate(),
            new RebaseCurrentBranchTemplate()
        });

    private static Prompt SeedPrompt(
        FakeApplicationDbContext context,
        Guid ownerId,
        string content = "Content")
    {
        var directory = new WorkingDirectory
        {
            Id = Guid.CreateVersion7(),
            Name = "repo",
            AbsolutePath = "C:/repo",
            OwnerId = ownerId
        };
        var prompt = new Prompt
        {
            Id = Guid.CreateVersion7(),
            WorkingDirectoryId = directory.Id,
            Title = "Prompt",
            Content = content,
            OwnerId = ownerId
        };

        context.WorkingDirectoryItems.Add(directory);
        context.PromptItems.Add(prompt);
        return prompt;
    }

    private static LinkedDocument SeedLinkedDocument(
        FakeApplicationDbContext context,
        Prompt prompt,
        string absolutePath,
        string displayName)
    {
        var document = new LinkedDocument
        {
            Id = Guid.CreateVersion7(),
            PromptId = prompt.Id,
            WorkingDirectoryId = prompt.WorkingDirectoryId,
            AbsolutePath = absolutePath,
            AbsolutePathKey = absolutePath.ToLowerInvariant(),
            DisplayName = displayName,
            Status = LinkedDocumentStatus.Tracking,
            CurrentVersion = 1
        };

        context.LinkedDocumentItems.Add(document);
        return document;
    }

    private sealed class FakeApplicationDbContext : IApplicationDbContext
    {
        public List<User> UserItems { get; } = new();
        public List<WorkingDirectory> WorkingDirectoryItems { get; } = new();
        public List<Prompt> PromptItems { get; } = new();
        public List<PromptVersion> PromptVersionItems { get; } = new();
        public List<PromptFileReference> PromptFileReferenceItems { get; } = new();
        public List<LinkedDocument> LinkedDocumentItems { get; } = new();
        public List<LinkedDocumentVersion> LinkedDocumentVersionItems { get; } = new();
        public int SaveChangesCount { get; private set; }

        public IQueryable<User> Users => UserItems.AsQueryable();
        public IQueryable<WorkingDirectory> WorkingDirectories => WorkingDirectoryItems.AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTask> FutureTasks => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTask>().AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTaskLabel> FutureTaskLabels => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTaskLabel>().AsQueryable();
        public IQueryable<Prompt> Prompts => PromptItems.AsQueryable();
        public IQueryable<PromptVersion> PromptVersions => PromptVersionItems.AsQueryable();
        public IQueryable<PromptFileReference> PromptFileReferences => PromptFileReferenceItems.AsQueryable();
        public IQueryable<LinkedDocument> LinkedDocuments => LinkedDocumentItems.AsQueryable();
        public IQueryable<LinkedDocumentVersion> LinkedDocumentVersions => LinkedDocumentVersionItems.AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.WorkflowTemplate> WorkflowTemplates => Enumerable.Empty<Thoth.Domain.Workflows.WorkflowTemplate>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.WorkflowTemplatePhase> WorkflowTemplatePhases => Enumerable.Empty<Thoth.Domain.Workflows.WorkflowTemplatePhase>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.PromptWorkflow> PromptWorkflows => Enumerable.Empty<Thoth.Domain.Workflows.PromptWorkflow>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.PromptWorkflowPhase> PromptWorkflowPhases => Enumerable.Empty<Thoth.Domain.Workflows.PromptWorkflowPhase>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.PromptWorkflowEvent> PromptWorkflowEvents => Enumerable.Empty<Thoth.Domain.Workflows.PromptWorkflowEvent>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiChatSession> AiChatSessions => Enumerable.Empty<Thoth.Domain.Ai.AiChatSession>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiChatMessage> AiChatMessages => Enumerable.Empty<Thoth.Domain.Ai.AiChatMessage>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiUserSettings> AiUserSettings => Enumerable.Empty<Thoth.Domain.Ai.AiUserSettings>().AsQueryable();
        public IQueryable<Thoth.Domain.Notebooks.Notebook> Notebooks => Enumerable.Empty<Thoth.Domain.Notebooks.Notebook>().AsQueryable();
        public IQueryable<Thoth.Domain.Notebooks.Note> Notes => Enumerable.Empty<Thoth.Domain.Notebooks.Note>().AsQueryable();
        public IQueryable<Thoth.Domain.Diagrams.Diagram> Diagrams => Enumerable.Empty<Thoth.Domain.Diagrams.Diagram>().AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class
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
                case LinkedDocument document:
                    LinkedDocumentItems.Add(document);
                    break;
                case LinkedDocumentVersion version:
                    LinkedDocumentVersionItems.Add(version);
                    break;
            }
        }

        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities)
            {
                Add(entity);
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
                case LinkedDocument document:
                    LinkedDocumentItems.Remove(document);
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
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => User.SystemUserId;
    }
}
