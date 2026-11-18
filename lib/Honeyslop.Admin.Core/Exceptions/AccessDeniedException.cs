namespace Honeyslop.Admin.Core.Exceptions;

public sealed class AccessDeniedException(string message) : Exception(message);
