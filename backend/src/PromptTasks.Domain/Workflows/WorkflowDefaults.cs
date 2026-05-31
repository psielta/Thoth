namespace PromptTasks.Domain.Workflows;

public sealed record WorkflowPhaseSeed(string Name, WorkflowActor DefaultActor, string Color);

public static class WorkflowDefaults
{
    public const string TemplateName = "Fluxo padrão";

    public static IReadOnlyList<WorkflowPhaseSeed> Phases { get; } =
    [
        new("Planejamento", WorkflowActor.ClaudeCode, "#2563eb"),
        new("Revisão do plano", WorkflowActor.Codex, "#7c3aed"),
        new("Correção do plano", WorkflowActor.ClaudeCode, "#d97706"),
        new("Implementação", WorkflowActor.Codex, "#0d9488"),
        new("Revisão de código", WorkflowActor.ClaudeCode, "#0891b2"),
        new("Teste prático", WorkflowActor.Human, "#db2777"),
        new("Commit/Merge", WorkflowActor.Human, "#16a34a")
    ];

    public static WorkflowTemplate BuildTemplate(Guid ownerId)
    {
        var template = new WorkflowTemplate
        {
            Name = TemplateName,
            IsDefault = true,
            OwnerId = ownerId
        };

        var orderIndex = 0;
        foreach (var phase in Phases)
        {
            template.Phases.Add(new WorkflowTemplatePhase
            {
                WorkflowTemplateId = template.Id,
                Name = phase.Name,
                DefaultActor = phase.DefaultActor,
                OrderIndex = orderIndex++,
                Color = phase.Color
            });
        }

        return template;
    }
}
