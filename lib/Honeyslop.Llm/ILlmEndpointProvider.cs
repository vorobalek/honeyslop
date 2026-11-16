namespace Honeyslop.Llm;

public interface ILlmEndpointProvider
{
    ValueTask<IReadOnlyList<Uri>> GetEndpointsAsync(CancellationToken cancellationToken);
}
