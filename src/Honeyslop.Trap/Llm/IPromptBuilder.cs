using Honeyslop.Data.Entities;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Trap.Llm;

public interface IPromptBuilder
{
    LlmPrompt Build(Entry entry, bool allowRefusals);
}