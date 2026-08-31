namespace HcbeApi.Models;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTime Date { get; set; }
    public string? Location { get; set; }
    public string? LocationEn { get; set; }
    public string? Type { get; set; }
    public string? Zone { get; set; }
    public int? Capacity { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public string? MeetingLink { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "À venir";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<EventMedia> Media { get; set; } = new List<EventMedia>();
    public ICollection<EventAttachment> Attachments { get; set; } = new List<EventAttachment>();
}

