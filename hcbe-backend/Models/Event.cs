namespace HcbeApi.Models;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTime Date { get; set; }
    public DateTime? EndDate { get; set; }
    public string TimeZone { get; set; } = "America/Toronto";
    public string? Location { get; set; }
    public string? LocationEn { get; set; }
    public string? Type { get; set; }
    public string Format { get; set; } = "InPerson";
    public string? Zone { get; set; }
    public int? Capacity { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public string? MeetingLink { get; set; }
    public string? RegistrationUrl { get; set; }
    public string? CtaLabel { get; set; }
    public string? CtaLabelEn { get; set; }
    public string RegistrationMode { get; set; } = "External";
    public bool AllowWaitlist { get; set; } = true;
    public bool RestrictMeetingLinkToRegistrants { get; set; }
    public bool TicketingEnabled { get; set; }
    public string SalesModel { get; set; } = "HCBE";
    public Guid? CommunityOrganizerId { get; set; }
    public CommunityOrganizer? CommunityOrganizer { get; set; }
    public int PlatformFeePercent { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "À venir";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<EventSpeaker> Speakers { get; set; } = new List<EventSpeaker>();
    public ICollection<EventOrganizer> Organizers { get; set; } = new List<EventOrganizer>();
    public ICollection<EventMedia> Media { get; set; } = new List<EventMedia>();
    public ICollection<EventAttachment> Attachments { get; set; } = new List<EventAttachment>();
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    public ICollection<EventCommunication> Communications { get; set; } = new List<EventCommunication>();
    public ICollection<EventTicketTier> TicketTiers { get; set; } = new List<EventTicketTier>();
    public ICollection<EventPromoCode> PromoCodes { get; set; } = new List<EventPromoCode>();
    public ICollection<EventTicketOrder> TicketOrders { get; set; } = new List<EventTicketOrder>();
}

