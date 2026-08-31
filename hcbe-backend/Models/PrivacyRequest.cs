namespace HcbeApi.Models;

public sealed class PrivacyRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Type { get; set; } = "Deletion";
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExecuteAfterUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? SubjectReference { get; set; }
    public string? FailureReason { get; set; }
}
