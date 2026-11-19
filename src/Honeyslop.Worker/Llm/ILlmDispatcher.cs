using Honeyslop.Llm.Contracts;

namespace Honeyslop.Worker.Llm;

public interface ILlmDispatcher
{
    Task<LlmCompletion> CompleteAsync(
        LlmPrompt prompt,
        TimeSpan timeLimit,
        IProgress<LlmProgress>? progress,
        Func<string, CancellationToken, Task>? dispatched,
        CancellationToken cancellationToken);
}