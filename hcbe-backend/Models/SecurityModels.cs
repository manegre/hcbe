namespace HcbeApi.Models;

public sealed class MfaChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public string AuthenticationMethod { get; set; } = "password";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SecurityIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "Reported";
    public string? AssignedTo { get; set; }
    public string? ContainmentActions { get; set; }
    public string? RootCause { get; set; }
    public string? CorrectiveActions { get; set; }
    public bool PersonalDataInvolved { get; set; }
    public int? EstimatedPeopleAffected { get; set; }
    public string? HarmRiskAssessment { get; set; }
    public bool CaiNotificationRequired { get; set; }
    public DateTime? CaiNotifiedAtUtc { get; set; }
    public DateTime? IndividualsNotifiedAtUtc { get; set; }
    public Guid ReportedByUserId { get; set; }
    public Guid? LastUpdatedByUserId { get; set; }
    public DateTime ReportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ContainedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public sealed class AdminAccessReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewedUserId { get; set; }
    public Guid ReviewedByUserId { get; set; }
    public string Decision { get; set; } = "Retain";
    public string? Notes { get; set; }
    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime NextReviewAtUtc { get; set; } = DateTime.UtcNow.AddMonths(3);
}
