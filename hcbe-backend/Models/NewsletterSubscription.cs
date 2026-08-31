namespace HcbeApi.Models;

public class NewsletterSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "fr";
    public DateTime ConsentAcceptedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = "home";
    public string UnsubscribeToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
