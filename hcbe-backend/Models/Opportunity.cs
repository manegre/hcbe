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
    public string? Region { get; set; }
    public bool IsRemote { get; set; }
    public string? Skills { get; set; }
    public string? Availability { get; set; }
    public string? Commitment { get; set; }
    public string? Requirements { get; set; }
    public string? RequirementsEn { get; set; }
    public string? Benefits { get; set; }
    public string? BenefitsEn { get; set; }
    public string? ContactEmail { get; set; }
    public string? ApplyUrl { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
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
    public string? Experience { get; set; }
    public string? Availability { get; set; }
    public int MatchScore { get; set; }
    public string? MatchReasons { get; set; }
    public string Status { get; set; } = "Submitted";
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OpportunityApplicationDocument> Documents { get; set; } = new List<OpportunityApplicationDocument>();
    public ICollection<VolunteerTimeEntry> VolunteerTimeEntries { get; set; } = new List<VolunteerTimeEntry>();
    public OpportunityCertificate? Certificate { get; set; }
}

public sealed class OpportunityApplicationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OpportunityApplicationId { get; set; }
    public OpportunityApplication? OpportunityApplication { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class VolunteerTimeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OpportunityApplicationId { get; set; }
    public OpportunityApplication? OpportunityApplication { get; set; }
    public DateTime ActivityDate { get; set; }
    public decimal Hours { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class OpportunityCertificate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OpportunityApplicationId { get; set; }
    public OpportunityApplication? OpportunityApplication { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string? ContributionSummary { get; set; }
    public decimal? ConfirmedHours { get; set; }
    public Guid IssuedByUserId { get; set; }
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
}
