namespace HcbeApi.Models;

public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Profession { get; set; }
    public string? Expertise { get; set; }
    public string? Interests { get; set; }
    public string? Availability { get; set; }
    public string? Zone { get; set; }
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

