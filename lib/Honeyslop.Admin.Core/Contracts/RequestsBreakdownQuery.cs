using Honeyslop.Admin.Core.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestsBreakdownQuery(RequestFilter Filter, BreakdownDimension By, int Top = 20);
