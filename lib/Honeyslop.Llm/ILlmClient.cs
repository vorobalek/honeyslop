using Honeyslop.Llm.Contracts;

namespace Honeyslop.Llm;

public interface ILlmClient
{
    Task<int?> GetIdleSlotsAsync(Uri endpoint, CancellationToken cancellationToken);
    Task<LlmServerStatus?> GetStatusAsync(Uri endpoint, CancellationToken cancellationToken);

    Task<LlmCompletion> CompleteAsync(
        Uri endpoint,
        LlmPrompt prompt,
        TimeSpan timeLimit,
        IProgress<LlmProgress>? progress,
        CancellationToken cancellationToken);
}