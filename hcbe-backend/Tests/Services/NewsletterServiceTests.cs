using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Xunit;

namespace HcbeApi.Tests.Services;

public class NewsletterServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NewsletterService _service;

    public NewsletterServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _service = new NewsletterService(_context);
    }

    [Fact]
    public async Task SubscribeAsync_WhenNewEmail_ShouldCreateActiveSubscription()
    {
        var result = await _service.SubscribeAsync(ValidRequest());

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Subscription successful");

        var stored = _context.NewsletterSubscriptions.Single();
        stored.Email.Should().Be("ada@example.com");
        stored.FullName.Should().Be("Ada Lovelace");
        stored.PreferredLanguage.Should().Be("fr");
        stored.IsActive.Should().BeTrue();
        stored.Source.Should().Be("home");
        stored.UnsubscribeToken.Should().NotBeNullOrWhiteSpace();
        _context.CommunicationConsentEvents.Should().ContainSingle(item =>
            item.Email == "ada@example.com" && item.Action == "OptIn" && item.Source == "home");
    }

    [Fact]
    public async Task SubscribeAsync_WhenDuplicateActive_ShouldReturnGenericSuccessWithoutDuplicating()
    {
        await _service.SubscribeAsync(ValidRequest());
        var result = await _service.SubscribeAsync(ValidRequest() with { FullName = "Other Name" });

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Subscription successful");
        _context.NewsletterSubscriptions.Should().HaveCount(1);
        _context.NewsletterSubscriptions.Single().FullName.Should().Be("Ada Lovelace");
        _context.CommunicationConsentEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task SubscribeAsync_WhenInactive_ShouldReactivateAndUpdate()
    {
        await _service.SubscribeAsync(ValidRequest());
        var existing = _context.NewsletterSubscriptions.Single();
        existing.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _service.SubscribeAsync(ValidRequest() with
        {
            FullName = "Ada Updated",
            PreferredLanguage = "en",
            Source = "footer"
        });

        result.Success.Should().BeTrue();
        var stored = _context.NewsletterSubscriptions.Single();
        stored.IsActive.Should().BeTrue();
        stored.FullName.Should().Be("Ada Updated");
        stored.PreferredLanguage.Should().Be("en");
        stored.Source.Should().Be("footer");
        _context.CommunicationConsentEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubscribeAsync_WithoutConsent_ShouldFail()
    {
        var result = await _service.SubscribeAsync(ValidRequest() with { ConsentAccepted = false });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Consent is required");
        _context.NewsletterSubscriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportActiveCsvAsync_ShouldIncludeOnlyActiveRows()
    {
        await _service.SubscribeAsync(ValidRequest());
        await _service.SubscribeAsync(ValidRequest() with { Email = "inactive@example.com", FullName = "Inactive User" });
        var inactive = _context.NewsletterSubscriptions.Single(s => s.Email == "inactive@example.com");
        inactive.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _service.ExportActiveCsvAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().Contain("ada@example.com");
        result.Data.Should().NotContain("inactive@example.com");
    }

    [Fact]
    public async Task UnsubscribeAsync_WithValidToken_ShouldDeactivateSubscription()
    {
        await _service.SubscribeAsync(ValidRequest());
        var stored = _context.NewsletterSubscriptions.Single();

        var result = await _service.UnsubscribeAsync(stored.UnsubscribeToken);

        result.Success.Should().BeTrue();
        _context.NewsletterSubscriptions.Single().IsActive.Should().BeFalse();
        _context.CommunicationConsentEvents.Should().Contain(item => item.Action == "OptOut");
    }

    [Fact]
    public async Task UnsubscribeAsync_WithCampaign_ShouldAttributeWithdrawalToDelivery()
    {
        await _service.SubscribeAsync(ValidRequest());
        var stored = _context.NewsletterSubscriptions.Single();
        var campaign = new NewsletterCampaign { Subject = "Info", Body = "Body" };
        _context.NewsletterCampaigns.Add(campaign);
        _context.NewsletterDeliveries.Add(new NewsletterDelivery
        {
            Campaign = campaign,
            CampaignId = campaign.Id,
            Recipient = stored.Email,
            TrackingToken = "tracking-token"
        });
        await _context.SaveChangesAsync();

        var result = await _service.UnsubscribeAsync(stored.UnsubscribeToken, campaign.Id);

        result.Success.Should().BeTrue();
        _context.NewsletterDeliveries.Single().UnsubscribedAtUtc.Should().NotBeNull();
        _context.CommunicationConsentEvents.Should().Contain(item =>
            item.Action == "OptOut" && item.Source == "campaign");
    }

    [Fact]
    public async Task UpdateActiveAsync_WithSameState_ShouldNotDuplicateConsentHistory()
    {
        await _service.SubscribeAsync(ValidRequest());
        var stored = _context.NewsletterSubscriptions.Single();

        await _service.UpdateActiveAsync(stored.Id, new UpdateNewsletterSubscriptionRequest(true));

        _context.CommunicationConsentEvents.Should().ContainSingle();
    }

    private static SubscribeNewsletterRequest ValidRequest() =>
        new("ada@example.com", "Ada Lovelace", "fr", true, "home");

    public void Dispose()
    {
        _context.Dispose();
    }
}
