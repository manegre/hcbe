namespace HcbeApi.Models;

public sealed class WebPushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string EndpointHash { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Unknown device";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAtUtc { get; set; } = DateTime.UtcNow;
}
