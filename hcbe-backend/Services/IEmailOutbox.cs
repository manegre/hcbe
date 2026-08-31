namespace HcbeApi.Services;

public interface IEmailOutbox
{
    void Enqueue(
        string recipient,
        string subject,
        string htmlBody,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null);
}
