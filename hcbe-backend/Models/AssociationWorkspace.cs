namespace HcbeApi.Models;

public class AssociationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssociationId { get; set; }
    public Association? Association { get; set; }
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Role { get; set; } = "Member";
    public string? Title { get; set; }
    public string Permissions { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class AssociationJoinRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssociationId { get; set; }
    public Association? Association { get; set; }
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

public class AssociationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssociationId { get; set; }
    public Association? Association { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string Visibility { get; set; } = "Members";
    public Guid UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AssociationCalendarItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssociationId { get; set; }
    public Association? Association { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Location { get; set; }
    public string? LocationEn { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
