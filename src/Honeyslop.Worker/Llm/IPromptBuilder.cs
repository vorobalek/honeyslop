using Honeyslop.Data.Entities;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Worker.Llm;

public interface IPromptBuilder
{
    LlmPrompt Build(Entry entry, bool allowRefusals);
}