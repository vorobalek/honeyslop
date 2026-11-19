using Honeyslop.Data.Requests;
using Honeyslop.Trap.Options;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Requests.Internals;

internal sealed class RequestClassifier(IOptions<TrapOptions> options) : IRequestClassifier
{
    public Classification Classify(string method, string target)
    {
        return options.Value.Requests.Classify(method, target);
    }
}