namespace HcbeApi.Models;

public class MentorshipApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ProfessionalSummary { get; set; } = string.Empty;
    public string Expertise { get; set; } = string.Empty;
    public string Objectives { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "fr";
    public bool ConsentToShare { get; set; }
    public string Status { get; set; } = "Pending";
    public string? CommitteeNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

public class MentorshipMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MentorApplicationId { get; set; }
    public MentorshipApplication? MentorApplication { get; set; }
    public Guid MenteeApplicationId { get; set; }
    public MentorshipApplication? MenteeApplication { get; set; }
    public string Status { get; set; } = "Proposed";
    public bool MentorAccepted { get; set; }
    public bool MenteeAccepted { get; set; }
    public string? CommitteeNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
