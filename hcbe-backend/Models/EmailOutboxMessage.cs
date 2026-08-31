namespace HcbeApi.Models;

public sealed class EmailOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LockedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? LastError { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
