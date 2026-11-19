namespace Honeyslop.Trap.Responses;

public interface IResponseSanitizer
{
    GeneratedResponse Sanitize(GeneratedResponse response, string host);
}