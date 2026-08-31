namespace HcbeApi.Models;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? ChangesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? TraceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
