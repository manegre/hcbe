namespace HcbeApi.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsAdmin { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

