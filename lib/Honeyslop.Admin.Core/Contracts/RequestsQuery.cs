using Honeyslop.Admin.Core.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestsQuery(RequestFilter Filter, Sorting<RequestColumn>? Sort = null, int Page = 0);
