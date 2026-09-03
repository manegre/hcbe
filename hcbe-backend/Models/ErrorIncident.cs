namespace HcbeApi.Models;

public sealed class ErrorIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Fingerprint { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstOccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastOccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAlertedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}
