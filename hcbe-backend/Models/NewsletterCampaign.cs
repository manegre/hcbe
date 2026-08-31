namespace HcbeApi.Models;

public class NewsletterCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = string.Empty;
    public string? SubjectEn { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? BodyEn { get; set; }
    public string Status { get; set; } = "Draft";
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public string? LastError { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
