using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record EntryResponseDto(int Status, ResponseHeader[] Headers, string Body);
