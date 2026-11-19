namespace Honeyslop.Llm.Exceptions;

public sealed class LlmUnavailableException(string message,
    Exception? innerException = null) : Exception(message, innerException);