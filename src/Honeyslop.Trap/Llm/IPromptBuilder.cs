using Honeyslop.Llm;
using Honeyslop.Data.Entities;

namespace Honeyslop.Trap.Llm;

public interface IPromptBuilder
{
    LlmPrompt Build(Entry entry, bool allowRefusals);
}
