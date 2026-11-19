namespace Honeyslop.Data.Enums;

public enum RequestOutcome
{
    Hit = 0,
    Generated = 1,
    Fallback = 2,
    Static = 3,
    Abandoned = 4,
    Error = 5,
    Waiting = 6
}