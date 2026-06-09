using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.PromptTemplates;

public interface IPromptTemplateCatalog
{
    IReadOnlyList<IPromptTemplateDefinition> GetAll();

    IPromptTemplateDefinition Get(PromptTemplateKey key);
}
