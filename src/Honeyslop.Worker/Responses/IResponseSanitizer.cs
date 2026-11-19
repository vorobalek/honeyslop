namespace Honeyslop.Worker.Responses;

public interface IResponseSanitizer
{
    GeneratedResponse Sanitize(GeneratedResponse response, string host);
}