using FluentAssertions;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Tests.Services;

public sealed class EmailOutboxTests
{
    [Fact]
    public async Task Enqueue_PersistsPendingMessageWithoutExposingDeliveryToRequest()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var outbox = new EmailOutbox(context);
        var relatedId = Guid.NewGuid();

        outbox.Enqueue(
            " User@Example.COM ",
            "Account update",
            "<p>Body</p>",
            "NewsletterCampaign",
            relatedId);
        await context.SaveChangesAsync();

        var message = await context.EmailOutboxMessages.SingleAsync();
        message.Recipient.Should().Be("user@example.com");
        message.Status.Should().Be("Pending");
        message.Attempts.Should().Be(0);
        message.RelatedEntityId.Should().Be(relatedId);
    }
}
