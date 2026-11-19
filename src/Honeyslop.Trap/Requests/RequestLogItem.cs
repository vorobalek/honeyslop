using Honeyslop.Data.Entities;

namespace Honeyslop.Trap.Requests;

public sealed record RequestLogItem(RequestRecord Record,
    bool IsUpdate);