using Honeyslop.Data.Entities;

namespace Honeyslop.Trap.Responses;

public sealed record GeneratedResponse(
    int Status,
    IReadOnlyList<ResponseHeader> Headers,
    string Body);
