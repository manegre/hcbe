using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HcbeApi.Tests.Services;

public sealed class FinanceServiceTests : IDisposable
{
    private readonly ApplicationDbContext context = TestDbContextFactory.CreateInMemoryContext();
    private readonly FakePaymentGateway gateway = new();
    private readonly RecordingOutbox outbox = new();

    [Fact]
    public async Task MembershipCheckout_CreatesPendingLedgerEntry_AndUsesHostedCheckout()
    {
        var (user, plan) = await AddMemberAndPlanAsync();
        var result = await CreateService().CreateMembershipCheckoutAsync(user.Id, new(plan.Id), default);

        result.Success.Should().BeTrue();
        result.Data!.CheckoutUrl.Should().Be("https://checkout.stripe.test/session");
        gateway.LastCheckout.Should().NotBeNull();
        gateway.LastCheckout!.Kind.Should().Be(FinanceKinds.Membership);
        gateway.LastCheckout.StripePriceId.Should().Be("price_hcbe_annual");
        context.FinancialTransactions.Should().ContainSingle(item =>
            item.UserId == user.Id && item.Status == FinanceStatuses.Pending && item.StripeCheckoutSessionId == "cs_test_hcbe");
    }

    [Fact]
    public async Task CompletedSubscriptionWebhook_IsIdempotent_AndInitialInvoiceIsNotCountedTwice()
    {
        var (user, plan) = await AddMemberAndPlanAsync();
        await CreateService().CreateMembershipCheckoutAsync(user.Id, new(plan.Id), default);
        var transaction = context.FinancialTransactions.Single();
        gateway.NextEvent = new("evt_checkout", "checkout.session.completed", $$$"""
            {"id":"cs_test_hcbe","payment_status":"paid","payment_intent":"pi_test","customer":"cus_test","subscription":"sub_test","invoice":"in_initial","amount_total":{{{plan.AmountCents}}},"metadata":{"hcbe_transaction_id":"{{{transaction.Id}}}"}}
            """);

        var service = CreateService();
        (await service.ProcessWebhookAsync("payload", "signature", default)).Success.Should().BeTrue();
        (await service.ProcessWebhookAsync("payload", "signature", default)).Success.Should().BeTrue();

        transaction.Status.Should().Be(FinanceStatuses.Paid);
        transaction.StripeInvoiceId.Should().Be("in_initial");
        context.MembershipStandings.Should().ContainSingle(item => item.UserId == user.Id && item.Status == MembershipStatuses.Active && item.AutoRenew);
        outbox.Count.Should().Be(1);

        gateway.NextEvent = new("evt_invoice", "invoice.paid", $$$"""
            {"id":"in_initial","subscription":"sub_test","customer":"cus_test","payment_intent":"pi_test","amount_paid":{{{plan.AmountCents}}},"currency":"cad"}
            """);
        (await service.ProcessWebhookAsync("payload", "signature", default)).Success.Should().BeTrue();
        context.FinancialTransactions.Should().ContainSingle();
        outbox.Count.Should().Be(1);
    }

    [Fact]
    public async Task DonationCheckout_ValidatesMinimum_AndNeverRecognizesAnonymousDonors()
    {
        var campaign = new DonationCampaign { Slug = "entraide", Title = "Fonds d’entraide", Description = "Soutien", IsPublished = true };
        context.DonationCampaigns.Add(campaign);
        await context.SaveChangesAsync();
        var service = CreateService();

        var invalid = await service.CreateDonationCheckoutAsync(null, new(campaign.Id, 200, "cad", "donor@example.com", "Awa", true, true, null, false), default);
        var valid = await service.CreateDonationCheckoutAsync(null, new(campaign.Id, 5000, "cad", "donor@example.com", "Awa", true, true, "Avec solidarité", false), default);

        invalid.Success.Should().BeFalse();
        valid.Success.Should().BeTrue();
        context.FinancialTransactions.Should().ContainSingle(item => item.IsAnonymous && !item.AllowPublicRecognition && item.DonorMessage == "Avec solidarité");
    }

    [Fact]
    public async Task InvalidWebhookSignature_DoesNotChangeTheLedger()
    {
        gateway.RejectWebhook = true;
        var result = await CreateService().ProcessWebhookAsync("payload", "invalid", default);

        result.Success.Should().BeFalse();
        context.PaymentWebhookEvents.Should().BeEmpty();
        context.FinancialTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task CompletedCheckout_WithDelayedPayment_RemainsPendingUntilAsyncSuccess()
    {
        var (user, plan) = await AddMemberAndPlanAsync();
        var service = CreateService();
        await service.CreateMembershipCheckoutAsync(user.Id, new(plan.Id), default);
        var transaction = context.FinancialTransactions.Single();
        gateway.NextEvent = new("evt_delayed", "checkout.session.completed", $$$"""
            {"id":"cs_test_hcbe","payment_status":"unpaid","customer":"cus_test","subscription":"sub_test","amount_total":{{{plan.AmountCents}}},"metadata":{"hcbe_transaction_id":"{{{transaction.Id}}}"}}
            """);

        (await service.ProcessWebhookAsync("payload", "signature", default)).Success.Should().BeTrue();
        transaction.Status.Should().Be(FinanceStatuses.Pending);
        context.MembershipStandings.Should().ContainSingle(item => item.UserId == user.Id && item.Status == MembershipStatuses.Inactive);
        outbox.Count.Should().Be(0);

        gateway.NextEvent = new("evt_delayed_success", "checkout.session.async_payment_succeeded", $$$"""
            {"id":"cs_test_hcbe","payment_status":"paid","payment_intent":"pi_delayed","customer":"cus_test","subscription":"sub_test","amount_total":{{{plan.AmountCents}}},"metadata":{"hcbe_transaction_id":"{{{transaction.Id}}}"}}
            """);
        (await service.ProcessWebhookAsync("payload", "signature", default)).Success.Should().BeTrue();

        transaction.Status.Should().Be(FinanceStatuses.Paid);
        context.MembershipStandings.Single(item => item.UserId == user.Id).Status.Should().Be(MembershipStatuses.Active);
        outbox.Count.Should().Be(1);
    }

    [Fact]
    public async Task CampaignTotals_IncludeNetPartiallyRefundedContributions()
    {
        var campaign = new DonationCampaign { Slug = "entraide", Title = "Fonds d’entraide", Description = "Soutien", IsPublished = true };
        context.DonationCampaigns.Add(campaign);
        context.FinancialTransactions.AddRange(
            new FinancialTransaction { DonationCampaignId = campaign.Id, Status = FinanceStatuses.Paid, AmountCents = 5000, PayerEmail = "one@example.com", ReceiptNumber = "ONE", ReceiptToken = "one" },
            new FinancialTransaction { DonationCampaignId = campaign.Id, Status = FinanceStatuses.PartiallyRefunded, AmountCents = 4000, RefundedAmountCents = 1000, PayerEmail = "two@example.com", ReceiptNumber = "TWO", ReceiptToken = "two" },
            new FinancialTransaction { DonationCampaignId = campaign.Id, Status = FinanceStatuses.Refunded, AmountCents = 2000, RefundedAmountCents = 2000, PayerEmail = "three@example.com", ReceiptNumber = "THREE", ReceiptToken = "three" });
        await context.SaveChangesAsync();

        var campaigns = await CreateService().GetCampaignsAsync(false, default);

        campaigns.Data.Should().ContainSingle();
        campaigns.Data![0].RaisedAmountCents.Should().Be(8000);
        campaigns.Data[0].SupporterCount.Should().Be(2);
    }

    [Fact]
    public async Task FullMembershipRefund_IsIdempotentAndCancelsAutomaticRenewal()
    {
        var (user, plan) = await AddMemberAndPlanAsync();
        var service = CreateService();
        await service.CreateMembershipCheckoutAsync(user.Id, new(plan.Id), default);
        var transaction = context.FinancialTransactions.Single();
        gateway.NextEvent = new("evt_paid_for_refund", "checkout.session.completed", $$$"""
            {"id":"cs_test_hcbe","payment_status":"paid","payment_intent":"pi_refund","customer":"cus_test","subscription":"sub_refund","amount_total":{{{plan.AmountCents}}},"metadata":{"hcbe_transaction_id":"{{{transaction.Id}}}"}}
            """);
        await service.ProcessWebhookAsync("payload", "signature", default);

        var result = await service.RefundAsync(transaction.Id, new(null, "Member request"), default);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(FinanceStatuses.Refunded);
        gateway.LastRefundIdempotencyKey.Should().Be($"hcbe-refund-{transaction.Id:N}-0-{plan.AmountCents}");
        gateway.CancelledSubscriptionId.Should().Be("sub_refund");
        context.MembershipStandings.Should().ContainSingle(item =>
            item.UserId == user.Id && item.Status == MembershipStatuses.Inactive && !item.AutoRenew && item.StripeSubscriptionId == null);
    }

    [Fact]
    public async Task PendingProviderRefund_DoesNotReduceLedgerRevenueBeforeConfirmation()
    {
        await AddMemberAndPlanAsync();
        var transaction = new FinancialTransaction
        {
            Status = FinanceStatuses.Paid,
            AmountCents = 5000,
            PayerEmail = "donor@example.com",
            StripePaymentIntentId = "pi_pending_refund",
            ReceiptNumber = "PENDING",
            ReceiptToken = "pending"
        };
        context.FinancialTransactions.Add(transaction);
        await context.SaveChangesAsync();
        gateway.NextRefundStatus = "pending";

        var result = await CreateService().RefundAsync(transaction.Id, new(null, "Member request"), default);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("awaiting confirmation");
        transaction.Status.Should().Be(FinanceStatuses.Paid);
        transaction.RefundedAmountCents.Should().Be(0);
        context.Notifications.Should().ContainSingle(item => item.Type == "finance-alert" && item.RelatedEntityId == transaction.Id);
    }

    [Fact]
    public async Task SubscriptionUpdate_FromBillingPortalDisablesAutomaticRenewal()
    {
        var (user, plan) = await AddMemberAndPlanAsync();
        var standing = new MembershipStanding
        {
            UserId = user.Id,
            PlanId = plan.Id,
            Status = MembershipStatuses.Active,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(8),
            AutoRenew = true,
            StripeSubscriptionId = "sub_portal"
        };
        context.MembershipStandings.Add(standing);
        await context.SaveChangesAsync();
        gateway.NextEvent = new("evt_subscription_update", "customer.subscription.updated", """
            {"id":"sub_portal","status":"active","cancel_at_period_end":true}
            """);

        var result = await CreateService().ProcessWebhookAsync("payload", "signature", default);

        result.Success.Should().BeTrue();
        standing.AutoRenew.Should().BeFalse();
        standing.Status.Should().Be(MembershipStatuses.Active);
    }

    [Fact]
    public async Task MembershipCheckout_DoesNotCreateASecondRecurringSubscription()
    {
        var (user, plan) = await AddMemberAndPlanAsync();
        context.MembershipStandings.Add(new MembershipStanding
        {
            UserId = user.Id,
            PlanId = plan.Id,
            Status = MembershipStatuses.Active,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(6),
            AutoRenew = true,
            StripeSubscriptionId = "sub_existing"
        });
        await context.SaveChangesAsync();

        var result = await CreateService().CreateMembershipCheckoutAsync(user.Id, new(plan.Id), default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("billing portal");
        context.FinancialTransactions.Should().BeEmpty();
        gateway.LastCheckout.Should().BeNull();
    }

    [Fact]
    public async Task FinanceAdministrator_CanSetAndFindAMembersStanding()
    {
        var (user, _) = await AddMemberAndPlanAsync();
        var expires = DateTime.UtcNow.AddMonths(9);
        var service = CreateService();

        var updated = await service.UpdateMembershipAsync(user.Id, new(MembershipStatuses.Active, expires, "manual payment"), default);
        var rows = await service.GetMembershipsAsync("awa@example.com", default);

        updated.Success.Should().BeTrue();
        rows.Data.Should().ContainSingle(item => item.UserId == user.Id && item.Status == MembershipStatuses.Active);
    }

    private async Task<(User User, MembershipPlan Plan)> AddMemberAndPlanAsync()
    {
        var member = new Member { FirstName = "Awa", LastName = "Sawadogo", Email = "awa@example.com" };
        var user = new User { Email = member.Email, FirstName = member.FirstName, LastName = member.LastName, Member = member, MemberId = member.Id, IsActive = true };
        var plan = new MembershipPlan { Name = "Membre annuel", Description = "Adhésion HCBE", AmountCents = 5000, Currency = "cad", BillingMode = "Recurring", StripePriceId = "price_hcbe_annual" };
        context.AddRange(user, plan);
        await context.SaveChangesAsync();
        return (user, plan);
    }

    private FinanceService CreateService()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicAppUrl"] = "https://hcbe.ca",
            ["PublicApiUrl"] = "https://api.hcbe.ca",
            ["JwtSettings:Secret"] = "a-test-signing-secret-longer-than-thirty-two-characters"
        }).Build();
        return new FinanceService(context, gateway, outbox, new StubEmailRenderer(),
            Options.Create(new FinanceOptions { Enabled = true, MinimumDonationCents = 500, MembershipGracePeriodDays = 30, Currency = "cad" }),
            configuration, NullLogger<FinanceService>.Instance);
    }

    public void Dispose() => context.Dispose();

    private sealed class FakePaymentGateway : IPaymentGateway
    {
        public bool IsEnabled => true;
        public bool RejectWebhook { get; set; }
        public string NextRefundStatus { get; set; } = "succeeded";
        public string? LastRefundIdempotencyKey { get; private set; }
        public string? CancelledSubscriptionId { get; private set; }
        public PaymentCheckoutRequest? LastCheckout { get; private set; }
        public VerifiedPaymentEvent NextEvent { get; set; } = new("evt_default", "ignored", "{}");
        public Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken)
        {
            LastCheckout = request;
            return Task.FromResult(new PaymentCheckoutResult("cs_test_hcbe", "https://checkout.stripe.test/session", "cus_test"));
        }
        public Task<string> CreateBillingPortalAsync(string customerId, string returnUrl, CancellationToken cancellationToken) => Task.FromResult("https://billing.stripe.test/session");
        public Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken)
        {
            CancelledSubscriptionId = subscriptionId;
            return Task.CompletedTask;
        }
        public Task<PaymentRefundResult> RefundAsync(string paymentIntentId, long? amountCents, string? reason, string idempotencyKey, CancellationToken cancellationToken)
        {
            LastRefundIdempotencyKey = idempotencyKey;
            return Task.FromResult(new PaymentRefundResult("re_test", NextRefundStatus, amountCents ?? 5000));
        }
        public VerifiedPaymentEvent VerifyWebhook(string payload, string signature) => RejectWebhook ? throw new InvalidOperationException("bad signature") : NextEvent;
    }

    private sealed class RecordingOutbox : IEmailOutbox
    {
        public int Count { get; private set; }
        public void Enqueue(string recipient, string subject, string htmlBody, string? relatedEntityType = null, Guid? relatedEntityId = null) => Count++;
    }

    private sealed class StubEmailRenderer : IEmailTemplateRenderer
    {
        private static RenderedEmail Email(string subject) => new(subject, "<p>HCBE</p>");
        public RenderedEmail MemberOnboarding(string? firstName, string actionUrl) => Email("onboarding");
        public RenderedEmail MemberWelcome(string? firstName, string loginUrl) => Email("member");
        public RenderedEmail AdminWelcome(string? firstName, string email, string temporaryPassword, string adminLoginUrl) => Email("welcome");
        public RenderedEmail AdminPromotion(string? firstName, string adminLoginUrl) => Email("promotion");
        public RenderedEmail PasswordReset(string? firstName, string resetUrl, int expiresInMinutes) => Email("reset");
        public RenderedEmail PasswordChanged(string? firstName, string memberSpaceUrl) => Email("changed");
        public RenderedEmail MembershipDecision(string? firstName, bool approved, string actionUrl) => Email("decision");
        public RenderedEmail Newsletter(string subject, string body, string unsubscribeUrl, bool useEnglish) => Email(subject);
        public RenderedEmail EventRegistrationUpdate(string? firstName, string eventTitle, DateTime eventDate, string status, string confirmationCode, string eventUrl) => Email("event");
        public RenderedEmail EventMessage(string? firstName, string eventTitle, string subject, string body, string eventUrl) => Email(subject);
        public RenderedEmail ServiceCaseUpdate(string? firstName, string ticketNumber, string subject, string status, string? message, string caseUrl) => Email("service");
        public RenderedEmail MembershipReminder(string? firstName, DateTime expiresAtUtc, string renewalUrl, bool expired) => Email("renewal");
        public RenderedEmail PaymentReceipt(string? name, string kind, long amountCents, string currency, string receiptNumber, string receiptUrl) => Email("receipt");
    }
}
