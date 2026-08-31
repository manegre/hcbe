using HcbeApi.Data;
using HcbeApi.Models;

namespace HcbeApi.Services;

public sealed class EmailOutbox(ApplicationDbContext context) : IEmailOutbox
{
    public void Enqueue(
        string recipient,
        string subject,
        string htmlBody,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null)
    {
        context.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            Recipient = recipient.Trim().ToLowerInvariant(),
            Subject = subject.Trim(),
            HtmlBody = htmlBody,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId
        });
    }
}
