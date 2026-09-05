using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HcbeApi.Tests.Services;

public sealed class NewsletterCampaignServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly NewsletterCampaignService _service;
    private readonly Mock<IAppPushService> _push = new();

    public NewsletterCampaignServiceTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicApiUrl"] = "https://api.hcbe.test",
            ["PublicAppUrl"] = "https://hcbe.test"
        }).Build();
        _push.Setup(service => service.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new NewsletterCampaignService(
            _context,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration,
            _push.Object);
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

    [Fact]
    public async Task MemberCampaign_PreviewsAndDeliversAcrossSelectedChannels()
    {
        var member = new Member { FirstName = "Awa", LastName = "Test", Email = "awa@example.com", Province = "Québec", Zone = "Est", Interests = "Mentorat" };
        var user = new User { Email = member.Email, PasswordHash = "hash", Member = member, MemberId = member.Id, IsActive = true };
        var association = new Association { Name = "Comité test", Province = "Québec", City = "Montréal" };
        _context.AddRange(member, user, association,
            new MemberPreference { UserId = user.Id, PreferredLanguage = "fr", HasCompletedPreferences = true, EmailEvents = true, PushNotifications = true },
            new MembershipStanding { UserId = user.Id, Status = MembershipStatuses.Active },
            new AssociationMember { AssociationId = association.Id, MemberId = member.Id, Status = "Active" },
            new WebPushSubscription { UserId = user.Id, Endpoint = "https://push.example.test/1", EndpointHash = "hash", P256dh = "key", Auth = "auth" });
        await _context.SaveChangesAsync();
        var request = new CreateNewsletterCampaignRequest("Événement", "Event", "Bienvenue", "Welcome", "Members", "Email,InApp,Push", "events", "Québec", "Est", "fr", "Mentorat", MembershipStatuses.Active, association.Id);

        var preview = await _service.PreviewAsync(request, CancellationToken.None);
        var created = await _service.CreateAsync(request, user.Id);
        var sent = await _service.SendAsync(created.Data!.Id, CancellationToken.None);

        preview.Data.Should().Be(new CampaignAudiencePreviewDto(1, 1, 1, 1));
        sent.Success.Should().BeTrue();
        sent.Data!.InAppSentCount.Should().Be(1);
        sent.Data.PushSentCount.Should().Be(1);
        _context.Notifications.Should().ContainSingle(item => item.UserId == user.Id);
        var delivery = _context.NewsletterDeliveries.Should().ContainSingle().Subject;
        delivery.EmailStatus.Should().Be("Queued");
        delivery.InAppStatus.Should().Be("Delivered");
        delivery.PushStatus.Should().Be("Delivered");
    }

    public void Dispose() => _context.Dispose();
}
