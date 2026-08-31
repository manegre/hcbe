namespace HcbeApi.Models;

public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? PositionEn { get; set; }
    public string Region { get; set; } = string.Empty;
    public string? RegionEn { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string? ZoneEn { get; set; }
    public string? Photo { get; set; }
    public string? Bio { get; set; }
    public string? BioEn { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
