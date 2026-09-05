namespace HcbeApi.Models;

public sealed class EventSurveyResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventRegistrationId { get; set; }
    public EventRegistration EventRegistration { get; set; } = null!;
    public int Rating { get; set; }
    public string? Feedback { get; set; }
    public bool ConsentToQuote { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EventCommunication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid SentByUserId { get; set; }
    public string Audience { get; set; } = "Active";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
