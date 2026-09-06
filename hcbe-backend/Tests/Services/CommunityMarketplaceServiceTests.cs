using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HcbeApi.Tests.Services;

public sealed class CommunityMarketplaceServiceTests : IDisposable
{
    private readonly ApplicationDbContext context = TestDbContextFactory.CreateInMemoryContext();
    private readonly ConnectGateway stripe = new();

    [Fact]
    public async Task ApprovedOrganizer_CreatesDraftTicketedEvent_WithCommunitySeller()
    {
        var user = new User { Email = "festival@example.com", IsActive = true };
        var organizer = new CommunityOrganizer { User = user, DisplayName = "Festival Canada", ContactEmail = user.Email, Status = OrganizerStatuses.Approved };
        context.AddRange(user, organizer); await context.SaveChangesAsync();

        var result = await Service().SaveOrganizerEventAsync(null, user.Id, new("Festival", "Festival", "Une célébration communautaire complète.", "A full community celebration.", DateTime.UtcNow.AddMonths(2), null, "Toronto", "Toronto", "InPerson", null, 3500, "cad", 500), default);

        result.Success.Should().BeTrue();
        var entity = context.Events.Single(); entity.Status.Should().Be("Draft"); entity.TicketingEnabled.Should().BeTrue(); entity.SalesModel.Should().Be("Community"); entity.CommunityOrganizerId.Should().Be(organizer.Id); entity.PlatformFeePercent.Should().Be(5);
        context.EventTicketTiers.Should().ContainSingle(item => item.PriceCents == 3500 && item.Quantity == 500);
    }

    [Fact]
    public async Task Advertising_IsInvisibleUntilApproved_AndTracksViewsAndClicks()
    {
        var user = new User { Email = "advertiser@example.com", IsActive = true };
        var organizer = new CommunityOrganizer { User = user, DisplayName = "Entreprise", ContactEmail = user.Email, Status = OrganizerStatuses.Approved };
        context.AddRange(user, organizer); await context.SaveChangesAsync();
        var request = new UpsertAdvertisingCampaignRequest("Entreprise", user.Email, "Service", "Service", "Découvrez notre service.", "Discover our service.", null, "https://example.com/offre", ["Events"], null, null, null, 10000, "cad", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(10));
        var created = await Service().SaveAdAsync(null, user.Id, request, default);

        (await Service().GetActiveAdsAsync("Events", "fr", null, null, default)).Data.Should().BeEmpty();
        (await Service().GetMyAdsAsync(user.Id, default)).Data.Should().ContainSingle(item => item.Status == "Submitted");
        await Service().ReviewAdAsync(created.Data!.Id, new("Approved", null), default);
        (await Service().GetActiveAdsAsync("Events", "fr", null, null, default)).Data.Should().ContainSingle();
        (await Service().TrackAdClickAsync(created.Data.Id, default))!.Host.Should().Be("example.com");
        var entity = context.AdvertisingCampaigns.Single(); entity.ImpressionCount.Should().Be(1); entity.ClickCount.Should().Be(1);
    }

    [Fact]
    public async Task StripeOnboarding_RequiresApproval_AndUsesHostedLink()
    {
        var user = new User { Email = "pending@example.com", IsActive = true };
        var organizer = new CommunityOrganizer { User = user, DisplayName = "Pending", ContactEmail = user.Email, Status = OrganizerStatuses.Pending };
        context.AddRange(user, organizer); await context.SaveChangesAsync();
        (await Service().CreateOnboardingAsync(user.Id, default)).Success.Should().BeFalse();
        organizer.Status = OrganizerStatuses.Approved; await context.SaveChangesAsync();

        var result = await Service().CreateOnboardingAsync(user.Id, default);

        result.Success.Should().BeTrue(); result.Data!.Url.Should().Be("https://connect.stripe.test/onboard"); organizer.StripeAccountId.Should().Be("acct_v2_test");
    }

    private CommunityMarketplaceService Service()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppUrl"] = "https://hcbe.test", ["CommunityMarketplace:PlatformFeePercent"] = "5" }).Build();
        return new(context, stripe, config, NullLogger<CommunityMarketplaceService>.Instance);
    }
    public void Dispose() => context.Dispose();
    private sealed class ConnectGateway : IStripeConnectGateway
    {
        public bool IsEnabled => true;
        public Task<string> CreateAccountAsync(CommunityOrganizer organizer, CancellationToken ct) => Task.FromResult("acct_v2_test");
        public Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken ct) => Task.FromResult("https://connect.stripe.test/onboard");
        public Task<(bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled)> GetStatusAsync(string accountId, CancellationToken ct) => Task.FromResult((false, false, false));
    }
}
