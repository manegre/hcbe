using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HcbeApi.Tests.Services;

public sealed class EventCommerceServiceTests : IDisposable
{
    private readonly ApplicationDbContext context = TestDbContextFactory.CreateInMemoryContext();
    private readonly Gateway gateway = new();

    [Fact]
    public async Task FreeCheckout_IssuesQrTickets_AndPdf()
    {
        var eventEntity = ActiveEvent();
        var tier = new EventTicketTier { Event = eventEntity, Name = "Admission", NameEn = "Admission", PriceCents = 0, Currency = "cad", Quantity = 10, MaxPerOrder = 4, SalesStartUtc = DateTime.UtcNow.AddDays(-1), SalesEndUtc = DateTime.UtcNow.AddDays(10) };
        context.AddRange(eventEntity, tier); await context.SaveChangesAsync();

        var result = await Service().CreateCheckoutAsync(null, eventEntity.Id, new("Awa Test", "awa@example.com", [new(tier.Id, 2)]), default);

        result.Success.Should().BeTrue(); result.Data!.Status.Should().Be(TicketOrderStatuses.Paid); result.Data.AccessToken.Should().HaveLength(64);
        context.EventTickets.Should().HaveCount(2).And.OnlyContain(item => item.TicketCode.StartsWith("TKT-") && item.Status == "Valid");
        var pdf = await Service().BuildTicketPdfAsync(result.Data.AccessToken, default);
        pdf.Content.Should().StartWith(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4"));
        gateway.LastCheckout.Should().BeNull();
    }

    [Fact]
    public async Task PaidCommunityCheckout_UsesDirectChargeAndPlatformFee()
    {
        var user = new User { Email = "organizer@example.com", IsActive = true };
        var organizer = new CommunityOrganizer { User = user, ContactEmail = user.Email, DisplayName = "Festival", Status = OrganizerStatuses.Approved, StripeAccountId = "acct_test", StripeChargesEnabled = true, StripePayoutsEnabled = true };
        var eventEntity = ActiveEvent(); eventEntity.SalesModel = "Community"; eventEntity.PlatformFeePercent = 5; eventEntity.CommunityOrganizer = organizer;
        var tier = new EventTicketTier { Event = eventEntity, Name = "VIP", PriceCents = 10_000, Currency = "cad", Quantity = 20, MaxPerOrder = 4, SalesStartUtc = DateTime.UtcNow.AddDays(-1), SalesEndUtc = DateTime.UtcNow.AddDays(10) };
        context.AddRange(user, organizer, eventEntity, tier); await context.SaveChangesAsync();

        var result = await Service().CreateCheckoutAsync(null, eventEntity.Id, new("Buyer", "buyer@example.com", [new(tier.Id, 2)]), default);

        result.Success.Should().BeTrue(); gateway.LastCheckout!.ConnectedAccountId.Should().Be("acct_test"); gateway.LastCheckout.ApplicationFeeAmountCents.Should().Be(1000);
        gateway.LastCheckout.Lines.Should().ContainSingle(item => item.UnitAmountCents == 20_000);
    }

    [Fact]
    public async Task Checkout_RejectsInventoryAboveRemainingQuantity()
    {
        var eventEntity = ActiveEvent();
        var tier = new EventTicketTier { Event = eventEntity, Name = "Admission", PriceCents = 1000, Currency = "cad", Quantity = 1, MaxPerOrder = 4, SalesStartUtc = DateTime.UtcNow.AddDays(-1), SalesEndUtc = DateTime.UtcNow.AddDays(10) };
        context.AddRange(eventEntity, tier); await context.SaveChangesAsync();

        var result = await Service().CreateCheckoutAsync(null, eventEntity.Id, new("Buyer", "buyer@example.com", [new(tier.Id, 2)]), default);

        result.Success.Should().BeFalse(); context.EventTicketOrders.Should().BeEmpty();
    }

    private EventCommerceService Service()
    {
        var outbox = new Mock<IEmailOutbox>(); var renderer = new Mock<IEmailTemplateRenderer>();
        renderer.Setup(item => item.EventMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new RenderedEmail("Tickets", "Body"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppUrl"] = "https://hcbe.test", ["PublicApiUrl"] = "https://api.hcbe.test" }).Build();
        return new(context, gateway, outbox.Object, renderer.Object, config, NullLogger<EventCommerceService>.Instance);
    }
    private static Event ActiveEvent() => new() { Title = "Festival", Date = DateTime.UtcNow.AddDays(20), RegistrationDeadline = DateTime.UtcNow.AddDays(10), Status = "Active", TicketingEnabled = true, RegistrationMode = "Disabled" };
    public void Dispose() => context.Dispose();

    private sealed class Gateway : IPaymentGateway
    {
        public bool IsEnabled => true; public PaymentCheckoutRequest? LastCheckout { get; private set; }
        public Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken) { LastCheckout = request; return Task.FromResult(new PaymentCheckoutResult("cs_test", "https://checkout.stripe.test", null)); }
        public Task<string> CreateBillingPortalAsync(string customerId, string returnUrl, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentRefundResult> RefundAsync(string paymentIntentId, long? amountCents, string? reason, string idempotencyKey, CancellationToken cancellationToken, string? connectedAccountId = null) => throw new NotSupportedException();
        public VerifiedPaymentEvent VerifyWebhook(string payload, string signature) => throw new NotSupportedException();
    }
}
