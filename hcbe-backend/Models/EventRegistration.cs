namespace HcbeApi.Models;

public class EventRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event? Event { get; set; }
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string ConfirmationCode { get; set; } = string.Empty;
    public string? AccessibilityNeeds { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public EventSurveyResponse? SurveyResponse { get; set; }
}
