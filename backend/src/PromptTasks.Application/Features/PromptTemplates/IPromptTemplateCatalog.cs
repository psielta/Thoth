using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.PromptTemplates;

public interface IPromptTemplateCatalog
{
    IReadOnlyList<IPromptTemplateDefinition> GetAll();

    IPromptTemplateDefinition Get(PromptTemplateKey key);
}
