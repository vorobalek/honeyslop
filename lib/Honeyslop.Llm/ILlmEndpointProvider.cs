namespace Honeyslop.Llm;

public interface ILlmEndpointProvider
{
    ValueTask<IReadOnlyList<LlmEndpoint>> GetEndpointsAsync(CancellationToken cancellationToken);
}
