namespace HcbeApi.Models;

public class MemberProfile
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Province { get; set; }
    public string? Zone { get; set; }
    public string MembershipStatus { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

