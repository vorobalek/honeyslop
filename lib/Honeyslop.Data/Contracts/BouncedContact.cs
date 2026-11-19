namespace Honeyslop.Data.Contracts;

public sealed record BouncedContact
{
    public string Recipient { get; init; } = null!;
    public int Bounces { get; init; }
    public int Reports { get; init; }
    public DateTimeOffset LastBouncedAt { get; init; }
    public long LastReportId { get; init; }
    public string? LastStatus { get; init; }
    public string? LastDiagnostic { get; init; }
}