using Honeyslop.Llm;
namespace Honeyslop.Trap.Llm;

public interface ILlmDispatcher
{
    Task<LlmCompletion> CompleteAsync(LlmPrompt prompt, TimeSpan timeLimit, CancellationToken cancellationToken);
}
