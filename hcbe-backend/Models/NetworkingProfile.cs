namespace HcbeApi.Models;

public class NetworkingProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Expertise { get; set; } = string.Empty;
    public string Sectors { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Province { get; set; }
    public bool IsVisible { get; set; }
    public bool AllowContactRequests { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ConnectionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequesterMemberId { get; set; }
    public Member? RequesterMember { get; set; }
    public Guid RecipientMemberId { get; set; }
    public Member? RecipientMember { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}
