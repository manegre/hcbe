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
    public string PreferenceCategory { get; set; } = "newsletter";
    public string? TargetProvince { get; set; }
    public string? TargetZone { get; set; }
    public string? TargetLanguage { get; set; }
    public string? TargetInterest { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public string? LastError { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public ICollection<NewsletterDelivery> Deliveries { get; set; } = new List<NewsletterDelivery>();
}
