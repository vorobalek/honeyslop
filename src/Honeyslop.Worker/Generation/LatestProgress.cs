using Honeyslop.Llm.Contracts;

namespace Honeyslop.Worker.Generation;

internal sealed class LatestProgress : IProgress<LlmProgress>
{
    private LlmProgress? _value;

    public LlmProgress? Value => Volatile.Read(ref _value);

    public void Report(LlmProgress value)
    {
        Volatile.Write(ref _value, value);
    }
}