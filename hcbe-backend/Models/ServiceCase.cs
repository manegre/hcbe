namespace HcbeApi.Models;

public class ServiceCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TicketNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted";
    public string Priority { get; set; } = "Normal";
    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public ICollection<ServiceCaseMessage> Messages { get; set; } = new List<ServiceCaseMessage>();
    public ICollection<ServiceCaseAttachment> Attachments { get; set; } = new List<ServiceCaseAttachment>();
}

public class ServiceCaseMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceCaseId { get; set; }
    public ServiceCase? ServiceCase { get; set; }
    public Guid AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ServiceCaseAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceCaseId { get; set; }
    public ServiceCase? ServiceCase { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
