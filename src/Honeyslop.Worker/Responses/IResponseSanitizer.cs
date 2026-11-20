using Honeyslop.Worker.Llm;

namespace Honeyslop.Worker.Responses;

public interface IResponseSanitizer
{
    GeneratedResponse Sanitize(GeneratedResponse response, string host, RequestProfile profile, bool mayRefuse);
}