namespace HcbeApi.Models;

public sealed class Opportunity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string Type { get; set; } = "Volunteer";
    public string Organization { get; set; } = "HCBE Canada";
    public string? Location { get; set; }
    public bool IsRemote { get; set; }
    public string? Skills { get; set; }
    public string? ApplyUrl { get; set; }
    public DateTime? DeadlineUtc { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OpportunityApplication> Applications { get; set; } = new List<OpportunityApplication>();
}

public sealed class OpportunityApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted";
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
