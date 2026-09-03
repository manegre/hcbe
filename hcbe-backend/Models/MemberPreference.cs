namespace HcbeApi.Models;

public sealed class MemberPreference
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string PreferredLanguage { get; set; } = "fr";
    public string TimeZone { get; set; } = "America/Toronto";
    public bool EmailEvents { get; set; } = true;
    public bool EmailOpportunities { get; set; } = true;
    public bool EmailMentorship { get; set; } = true;
    public bool EmailServiceUpdates { get; set; } = true;
    public bool EmailNewsletter { get; set; } = true;
    public bool PushNotifications { get; set; }
    public bool HasCompletedPreferences { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
