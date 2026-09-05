namespace HcbeApi.Models;

public class NewsletterCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = string.Empty;
    public string? SubjectEn { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? BodyEn { get; set; }
    public string Status { get; set; } = "Draft";
    public string Audience { get; set; } = "Newsletter";
    public string Channels { get; set; } = "Email";
    public string PreferenceCategory { get; set; } = "newsletter";
    public string? TargetProvince { get; set; }
    public string? TargetZone { get; set; }
    public string? TargetLanguage { get; set; }
    public string? TargetInterest { get; set; }
    public string? TargetMembershipStatus { get; set; }
    public Guid? TargetAssociationId { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int InAppSentCount { get; set; }
    public int PushSentCount { get; set; }
    public int PushFailedCount { get; set; }
    public int TestSentCount { get; set; }
    public string? LastError { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public ICollection<NewsletterDelivery> Deliveries { get; set; } = new List<NewsletterDelivery>();
}
