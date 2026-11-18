using Honeyslop.Admin.Core.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientsQuery(int Days = 7, Sorting<ClientColumn>? Sort = null, int Page = 0);
