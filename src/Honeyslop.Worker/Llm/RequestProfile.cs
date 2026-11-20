namespace Honeyslop.Worker.Llm;

public sealed record RequestProfile(ResponseKind Kind, string Subject, string Server, bool Refusable = false);