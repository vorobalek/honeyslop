using Honeyslop.Data.Entities;

namespace Honeyslop.Worker.Responses;

public sealed record GeneratedResponse(
    int Status,
    IReadOnlyList<ResponseHeader> Headers,
    string Body);