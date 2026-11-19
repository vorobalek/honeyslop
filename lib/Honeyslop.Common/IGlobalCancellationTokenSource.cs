namespace Honeyslop.Common;

public interface IGlobalCancellationTokenSource
{
    CancellationToken Token { get; }
}