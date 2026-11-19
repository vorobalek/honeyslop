namespace Honeyslop.Llm.Exceptions;

public sealed class LlmException(string message) : Exception(message);