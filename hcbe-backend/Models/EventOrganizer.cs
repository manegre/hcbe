namespace HcbeApi.Models;

public class EventOrganizer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
