using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class NewsletterCampaignServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly NewsletterCampaignService _service;

    public NewsletterCampaignServiceTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicApiUrl"] = "https://api.hcbe.test",
            ["PublicAppUrl"] = "https://hcbe.test"
        }).Build();
        _service = new NewsletterCampaignService(
            _context,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration);
    }

    [Fact]
    public async Task SendAsync_QueuesBilingualTrackedDelivery()
    {
        _context.NewsletterSubscriptions.Add(new NewsletterSubscription
        {
            Email = "member@example.com",
            FullName = "Test Member",
            PreferredLanguage = "en",
            ConsentAcceptedAt = DateTime.UtcNow,
            IsActive = true,
            Source = "footer",
            UnsubscribeToken = "unsubscribe-token"
        });
        var campaign = new NewsletterCampaign
        {
            Subject = "Bonjour",
            SubjectEn = "Hello",
            Body = "Nouvelles",
            BodyEn = "News",
            Audience = "Newsletter",
            PreferenceCategory = "newsletter"
        };
        _context.NewsletterCampaigns.Add(campaign);
        await _context.SaveChangesAsync();

        var result = await _service.SendAsync(campaign.Id, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.RecipientCount.Should().Be(1);
        var delivery = _context.NewsletterDeliveries.Should().ContainSingle().Subject;
        delivery.Recipient.Should().Be("member@example.com");
        var outbox = _context.EmailOutboxMessages.Should().ContainSingle().Subject;
        outbox.Subject.Should().Be("Hello");
        outbox.HtmlBody.Should().Contain($"/api/newsletter/track/open/{delivery.TrackingToken}.gif");
        outbox.HtmlBody.Should().Contain($"campaignId={campaign.Id}");
    }

    [Fact]
    public async Task TrackOpenAsync_CountsUniqueOpenAndTotalOpens()
    {
        var campaign = new NewsletterCampaign { Subject = "Info", Body = "Body", SentCount = 1 };
        var delivery = new NewsletterDelivery
        {
            Campaign = campaign,
            CampaignId = campaign.Id,
            Recipient = "member@example.com",
            TrackingToken = "secure-tracking-token"
        };
        _context.AddRange(campaign, delivery);
        await _context.SaveChangesAsync();

        await _service.TrackOpenAsync(delivery.TrackingToken, CancellationToken.None);
        await _service.TrackOpenAsync(delivery.TrackingToken, CancellationToken.None);

        delivery.OpenCount.Should().Be(2);
        delivery.FirstOpenedAtUtc.Should().NotBeNull();
        delivery.LastOpenedAtUtc.Should().NotBeNull();
        var metrics = (await _service.GetAllAsync()).Data!.Single();
        metrics.OpenedCount.Should().Be(1);
        metrics.OpenRate.Should().Be(100);
    }

    public void Dispose() => _context.Dispose();
}
