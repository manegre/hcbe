namespace HcbeApi.Models;

public sealed class NewsletterDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public NewsletterCampaign Campaign { get; set; } = null!;
    public string Recipient { get; set; } = string.Empty;
    public string TrackingToken { get; set; } = string.Empty;
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FirstOpenedAtUtc { get; set; }
    public DateTime? LastOpenedAtUtc { get; set; }
    public int OpenCount { get; set; }
    public DateTime? UnsubscribedAtUtc { get; set; }
}

public sealed class CommunicationConsentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Category { get; set; } = "newsletter";
    public string Action { get; set; } = "OptIn";
    public string Source { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CommunityJourneyState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string JourneyType { get; set; } = string.Empty;
    public DateTime LastTriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public int TriggerCount { get; set; } = 1;
}
