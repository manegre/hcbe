using System.Security.Cryptography;
using System.Text.Json;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HcbeApi.Services;

public sealed class FinanceService(
    ApplicationDbContext context,
    IPaymentGateway paymentGateway,
    IEmailOutbox emailOutbox,
    IEmailTemplateRenderer emailRenderer,
    IOptions<FinanceOptions> configuredOptions,
    IConfiguration configuration,
    ILogger<FinanceService> logger) : IFinanceService
{
    private readonly FinanceOptions options = configuredOptions.Value;
    private string PublicAppUrl => (configuration["PublicAppUrl"] ?? "https://hcbe.ca").TrimEnd('/');
    private string PublicApiUrl => (configuration["PublicApiUrl"] ?? PublicAppUrl).TrimEnd('/');

    public async Task<ApiResponse<IReadOnlyList<MembershipPlanDto>>> GetPlansAsync(bool admin, CancellationToken cancellationToken)
    {
        var query = context.MembershipPlans.AsNoTracking();
        if (!admin) query = query.Where(item => item.IsActive && item.Id == CommunityMembership.PlanId);
        var items = await query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.AmountCents).ToListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<MembershipPlanDto>>.SuccessResponse(items.Select(MapPlan).ToList());
    }

    public async Task<ApiResponse<MembershipPlanDto>> CreatePlanAsync(UpsertMembershipPlanRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidatePlan(request);
        if (validation != null) return ApiResponse<MembershipPlanDto>.ErrorResponse(validation);
        var item = request.BillingMode == CommunityMembership.BillingMode
            ? new MembershipPlan { Id = CommunityMembership.PlanId }
            : new MembershipPlan();
        if (await context.MembershipPlans.AnyAsync(value => value.Id == item.Id, cancellationToken))
            return ApiResponse<MembershipPlanDto>.ErrorResponse("The community membership plan already exists");
        ApplyPlan(item, request);
        context.MembershipPlans.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        return ApiResponse<MembershipPlanDto>.SuccessResponse(MapPlan(item));
    }

    public async Task<ApiResponse<MembershipPlanDto>> UpdatePlanAsync(Guid id, UpsertMembershipPlanRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidatePlan(request);
        if (validation != null) return ApiResponse<MembershipPlanDto>.ErrorResponse(validation);
        var item = await context.MembershipPlans.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (item == null) return ApiResponse<MembershipPlanDto>.ErrorResponse("Membership plan not found");
        ApplyPlan(item, request);
        await context.SaveChangesAsync(cancellationToken);
        return ApiResponse<MembershipPlanDto>.SuccessResponse(MapPlan(item));
    }

    public async Task<ApiResponse<IReadOnlyList<DonationCampaignDto>>> GetCampaignsAsync(bool admin, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = context.DonationCampaigns.AsNoTracking();
        if (!admin) query = query.Where(item => item.IsPublished && (item.StartsAtUtc == null || item.StartsAtUtc <= now) && (item.EndsAtUtc == null || item.EndsAtUtc >= now));
        var campaigns = await query.OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        var ids = campaigns.Select(item => item.Id).ToList();
        var totals = await context.FinancialTransactions.AsNoTracking()
            .Where(item => item.DonationCampaignId != null && ids.Contains(item.DonationCampaignId.Value) &&
                (item.Status == FinanceStatuses.Paid || item.Status == FinanceStatuses.PartiallyRefunded))
            .GroupBy(item => item.DonationCampaignId!.Value)
            .Select(group => new { Id = group.Key, Amount = group.Sum(item => item.AmountCents - item.RefundedAmountCents), Count = group.Count() })
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        return ApiResponse<IReadOnlyList<DonationCampaignDto>>.SuccessResponse(campaigns.Select(item => MapCampaign(item, totals.GetValueOrDefault(item.Id)?.Amount ?? 0, totals.GetValueOrDefault(item.Id)?.Count ?? 0)).ToList());
    }

    public async Task<ApiResponse<DonationCampaignDto>> CreateCampaignAsync(UpsertDonationCampaignRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateCampaignAsync(request, null, cancellationToken);
        if (validation != null) return ApiResponse<DonationCampaignDto>.ErrorResponse(validation);
        var item = new DonationCampaign();
        ApplyCampaign(item, request);
        context.DonationCampaigns.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        return ApiResponse<DonationCampaignDto>.SuccessResponse(MapCampaign(item, 0, 0));
    }

    public async Task<ApiResponse<DonationCampaignDto>> UpdateCampaignAsync(Guid id, UpsertDonationCampaignRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateCampaignAsync(request, id, cancellationToken);
        if (validation != null) return ApiResponse<DonationCampaignDto>.ErrorResponse(validation);
        var item = await context.DonationCampaigns.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (item == null) return ApiResponse<DonationCampaignDto>.ErrorResponse("Donation campaign not found");
        ApplyCampaign(item, request);
        await context.SaveChangesAsync(cancellationToken);
        var aggregate = await context.FinancialTransactions.AsNoTracking().Where(value => value.DonationCampaignId == id &&
                (value.Status == FinanceStatuses.Paid || value.Status == FinanceStatuses.PartiallyRefunded))
            .GroupBy(_ => 1).Select(group => new { Amount = group.Sum(value => value.AmountCents - value.RefundedAmountCents), Count = group.Count() }).SingleOrDefaultAsync(cancellationToken);
        return ApiResponse<DonationCampaignDto>.SuccessResponse(MapCampaign(item, aggregate?.Amount ?? 0, aggregate?.Count ?? 0));
    }

    public async Task<ApiResponse<MemberFinanceSummaryDto>> GetMemberSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().Include(item => item.Member).SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user == null) return ApiResponse<MemberFinanceSummaryDto>.ErrorResponse("Account not found");
        var standing = await GetOrCreateStandingAsync(userId, cancellationToken);
        RefreshStandingStatus(standing);
        await context.SaveChangesAsync(cancellationToken);
        var plans = await context.MembershipPlans.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ToListAsync(cancellationToken);
        var transactions = await context.FinancialTransactions.AsNoTracking().Include(item => item.DonationCampaign)
            .Where(item => item.UserId == userId).OrderByDescending(item => item.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
        return ApiResponse<MemberFinanceSummaryDto>.SuccessResponse(new MemberFinanceSummaryDto(
            MapStanding(standing, user, standing.Plan), plans.Select(MapPlan).ToList(), transactions.Select(MapTransaction).ToList()));
    }

    public async Task<ApiResponse<MembershipCardDto>> GetMembershipCardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().Include(item => item.Member)
            .SingleOrDefaultAsync(item => item.Id == userId && item.IsActive && item.MemberId != null, cancellationToken);
        if (user?.Member is null) return ApiResponse<MembershipCardDto>.ErrorResponse("Active membership not found");
        var standing = await GetOrCreateStandingAsync(userId, cancellationToken);
        RefreshStandingStatus(standing);
        await context.SaveChangesAsync(cancellationToken);
        var status = EffectiveStatus(standing);
        if (status is not (MembershipStatuses.Active or MembershipStatuses.GracePeriod))
            return ApiResponse<MembershipCardDto>.ErrorResponse("Membership card is available only to active members");
        var code = EncodeVerificationCode(user.Id);
        return ApiResponse<MembershipCardDto>.SuccessResponse(new(
            $"{user.Member.FirstName} {user.Member.LastName}".Trim(), user.Email, status,
            standing.Plan?.Name ?? "Membre communautaire", standing.Plan?.NameEn ?? "Community member",
            user.Member.CreatedAt, standing.CurrentPeriodEndUtc, code, $"{PublicAppUrl}/adhesion/verifier/{code}"));
    }

    public async Task<ApiResponse<MembershipWalletDto>> GetMembershipWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var card = await GetMembershipCardAsync(userId, cancellationToken);
        if (!card.Success || card.Data is null) return ApiResponse<MembershipWalletDto>.ErrorResponse(card.Message ?? "Membership card unavailable");
        var enabled = configuration.GetValue<bool>("WalletPasses:Enabled");
        if (!enabled)
            return ApiResponse<MembershipWalletDto>.SuccessResponse(new(false, false, null, false, null));

        static string? Resolve(string? template, MembershipCardDto value)
        {
            if (string.IsNullOrWhiteSpace(template)) return null;
            var candidate = template.Replace("{code}", Uri.EscapeDataString(value.VerificationCode), StringComparison.Ordinal)
                .Replace("{verificationUrl}", Uri.EscapeDataString(value.VerificationUrl), StringComparison.Ordinal);
            return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? candidate : null;
        }
        var apple = Resolve(configuration["WalletPasses:AppleAddUrlTemplate"], card.Data);
        var google = Resolve(configuration["WalletPasses:GoogleAddUrlTemplate"], card.Data);
        return ApiResponse<MembershipWalletDto>.SuccessResponse(new(true, apple != null, apple, google != null, google));
    }

    public async Task<ApiResponse<MembershipStandingDto>> RenewCommunityMembershipAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.Include(item => item.Member)
            .SingleOrDefaultAsync(item => item.Id == userId && item.IsActive && item.MemberId != null, cancellationToken);
        if (user == null) return ApiResponse<MembershipStandingDto>.ErrorResponse("Member account not found");

        var existing = await context.MembershipStandings.Include(item => item.Plan)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (existing?.Status == MembershipStatuses.Inactive)
            return ApiResponse<MembershipStandingDto>.ErrorResponse("This membership is suspended. Please contact HCBE Canada.");

        var standing = existing ?? CommunityMembership.CreateStanding(userId, DateTime.UtcNow);
        if (existing == null) context.MembershipStandings.Add(standing);

        var now = DateTime.UtcNow;
        if (standing.CurrentPeriodEndUtc <= now.AddDays(CommunityMembership.RenewalWindowDays))
        {
            var renewalStart = standing.CurrentPeriodEndUtc > now ? standing.CurrentPeriodEndUtc.Value : now;
            standing.PlanId = CommunityMembership.PlanId;
            standing.Status = MembershipStatuses.Active;
            standing.CurrentPeriodStartUtc = now;
            standing.CurrentPeriodEndUtc = renewalStart.AddYears(1);
            standing.GraceEndsAtUtc = standing.CurrentPeriodEndUtc.Value.AddDays(options.MembershipGracePeriodDays);
            standing.AutoRenew = false;
            standing.LastReminderKey = null;
            standing.UpdatedAtUtc = now;
            context.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = "membership",
                Title = "Adhésion renouvelée",
                Message = "Votre adhésion communautaire gratuite a été renouvelée pour un an.",
                Link = "/espace-membre?section=membership",
                RelatedEntityId = standing.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await context.Entry(standing).Reference(item => item.Plan).LoadAsync(cancellationToken);
        return ApiResponse<MembershipStandingDto>.SuccessResponse(MapStanding(standing, user, standing.Plan));
    }

    public async Task<ApiResponse<CheckoutSessionDto>> CreateMembershipCheckoutAsync(Guid userId, CreateMembershipCheckoutRequest request, CancellationToken cancellationToken)
    {
        var user = await context.Users.Include(item => item.Member).SingleOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);
        var plan = await context.MembershipPlans.SingleOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive, cancellationToken);
        if (user == null || plan == null) return ApiResponse<CheckoutSessionDto>.ErrorResponse("Account or membership plan not found");
        if (plan.Id == CommunityMembership.PlanId || plan.BillingMode == CommunityMembership.BillingMode || plan.AmountCents == 0)
            return ApiResponse<CheckoutSessionDto>.ErrorResponse("The community membership is free and does not require payment");
        var standing = await GetOrCreateStandingAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(standing.StripeSubscriptionId))
            return ApiResponse<CheckoutSessionDto>.ErrorResponse("An existing recurring membership must be managed from the billing portal");
        var transaction = NewTransaction(FinanceKinds.Membership, plan.AmountCents, plan.Currency, user.Email, $"{user.FirstName} {user.LastName}".Trim(), userId);
        transaction.MembershipPlanId = plan.Id;
        transaction.IsRecurring = plan.BillingMode.Equals("Recurring", StringComparison.OrdinalIgnoreCase);
        context.FinancialTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return await StartCheckoutAsync(transaction, plan.Name, plan.StripePriceId, standing.StripeCustomerId, cancellationToken);
    }

    public async Task<ApiResponse<CheckoutSessionDto>> CreateDonationCheckoutAsync(Guid? userId, CreateDonationCheckoutRequest request, CancellationToken cancellationToken)
    {
        var minimum = Math.Clamp(options.MinimumDonationCents, 100, 100_000);
        if (request.AmountCents < minimum || request.AmountCents > 10_000_000) return ApiResponse<CheckoutSessionDto>.ErrorResponse($"Donation must be between {minimum} and 10000000 cents");
        var currency = NormalizeCurrency(request.Currency);
        if (!currency.Equals(NormalizeCurrency(options.Currency), StringComparison.OrdinalIgnoreCase)) return ApiResponse<CheckoutSessionDto>.ErrorResponse("Unsupported currency");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email)) return ApiResponse<CheckoutSessionDto>.ErrorResponse("A valid email is required");
        if (request.Email.Trim().Length > 320 || request.Name?.Trim().Length > 160) return ApiResponse<CheckoutSessionDto>.ErrorResponse("Donor contact information is too long");
        if (request.Message?.Trim().Length > 500) return ApiResponse<CheckoutSessionDto>.ErrorResponse("Donor message may contain at most 500 characters");
        DonationCampaign? campaign = null;
        if (request.CampaignId is Guid campaignId)
        {
            var now = DateTime.UtcNow;
            campaign = await context.DonationCampaigns.SingleOrDefaultAsync(item => item.Id == campaignId && item.IsPublished && (item.StartsAtUtc == null || item.StartsAtUtc <= now) && (item.EndsAtUtc == null || item.EndsAtUtc >= now), cancellationToken);
            if (campaign == null) return ApiResponse<CheckoutSessionDto>.ErrorResponse("Donation campaign not found");
            if (request.IsRecurring && !campaign.AllowRecurring) return ApiResponse<CheckoutSessionDto>.ErrorResponse("This campaign does not accept recurring donations");
        }
        if (userId != null && !await context.Users.AnyAsync(item => item.Id == userId && item.IsActive, cancellationToken)) userId = null;
        var transaction = NewTransaction(FinanceKinds.Donation, request.AmountCents, currency, request.Email, request.Name, userId);
        transaction.DonationCampaignId = campaign?.Id;
        transaction.IsAnonymous = request.IsAnonymous;
        transaction.AllowPublicRecognition = !request.IsAnonymous && request.AllowPublicRecognition;
        transaction.DonorMessage = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim()[..Math.Min(request.Message.Trim().Length, 500)];
        transaction.IsRecurring = request.IsRecurring;
        context.FinancialTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return await StartCheckoutAsync(transaction, campaign?.Title ?? "Don à HCBE Canada", null, null, cancellationToken);
    }

    public async Task<ApiResponse<CheckoutResultDto>> GetCheckoutResultAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 255) return ApiResponse<CheckoutResultDto>.ErrorResponse("Checkout session not found");
        var item = await context.FinancialTransactions.AsNoTracking().SingleOrDefaultAsync(value => value.StripeCheckoutSessionId == sessionId, cancellationToken);
        if (item == null) return ApiResponse<CheckoutResultDto>.ErrorResponse("Checkout session not found");
        return ApiResponse<CheckoutResultDto>.SuccessResponse(new CheckoutResultDto(item.Status, item.Kind, item.AmountCents, item.Currency,
            item.Status == FinanceStatuses.Paid ? $"{PublicApiUrl}/api/finance/receipts/{item.ReceiptToken}" : null,
            item.Kind == FinanceKinds.Membership ? $"{PublicAppUrl}/espace-membre?section=membership" : $"{PublicAppUrl}/contribuer"));
    }

    public async Task<ApiResponse<BillingPortalDto>> CreateBillingPortalAsync(Guid userId, CancellationToken cancellationToken)
    {
        var standing = await context.MembershipStandings.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (standing?.StripeCustomerId == null) return ApiResponse<BillingPortalDto>.ErrorResponse("No billing account is available");
        try
        {
            var url = await paymentGateway.CreateBillingPortalAsync(standing.StripeCustomerId, $"{PublicAppUrl}/espace-membre?section=membership", cancellationToken);
            return ApiResponse<BillingPortalDto>.SuccessResponse(new BillingPortalDto(url));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to create billing portal for {UserId}", userId);
            return ApiResponse<BillingPortalDto>.ErrorResponse("Unable to open billing portal");
        }
    }

    public async Task<ApiResponse> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        VerifiedPaymentEvent paymentEvent;
        try { paymentEvent = paymentGateway.VerifyWebhook(payload, signature); }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Rejected invalid payment webhook");
            return ApiResponse.CreateError("Invalid webhook signature");
        }
        var record = await context.PaymentWebhookEvents.SingleOrDefaultAsync(item => item.ProviderEventId == paymentEvent.Id, cancellationToken);
        if (record?.Status == "Processed") return ApiResponse.CreateSuccess("Webhook already processed");
        if (record?.Status == "Processing" && record.ReceivedAtUtc > DateTime.UtcNow.AddMinutes(-5))
            return ApiResponse.CreateError("Webhook is already being processed");

        if (record == null)
        {
            record = new PaymentWebhookEvent { ProviderEventId = paymentEvent.Id, EventType = paymentEvent.Type };
            context.PaymentWebhookEvents.Add(record);
        }
        else
        {
            record.Status = "Processing";
            record.EventType = paymentEvent.Type;
            record.Error = null;
            record.ProcessedAtUtc = null;
            record.ReceivedAtUtc = DateTime.UtcNow;
        }

        try
        {
            // Claim the provider event before applying business side effects. The
            // unique provider-event index prevents concurrent deliveries from running twice.
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            return ApiResponse.CreateError("Webhook is already being processed");
        }

        try
        {
            using var objectDocument = JsonDocument.Parse(paymentEvent.ObjectJson);
            await ApplyWebhookAsync(paymentEvent.Type, objectDocument.RootElement, cancellationToken);
            record.Status = "Processed";
            record.ProcessedAtUtc = DateTime.UtcNow;
            record.Error = null;
            await context.SaveChangesAsync(cancellationToken);
            return ApiResponse.CreateSuccess("Webhook processed");
        }
        catch (Exception exception)
        {
            // Discard tracked business changes so the failure marker cannot persist
            // a partially applied payment or membership update.
            context.ChangeTracker.Clear();
            record = await context.PaymentWebhookEvents.SingleAsync(item => item.ProviderEventId == paymentEvent.Id, cancellationToken);
            record.Status = "Failed";
            record.Error = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            await context.SaveChangesAsync(cancellationToken);
            logger.LogError(exception, "Payment webhook {PaymentEventId} failed", paymentEvent.Id);
            return ApiResponse.CreateError("Webhook processing failed");
        }
    }

    public async Task<ApiResponse<FinanceDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var paid = await context.FinancialTransactions.AsNoTracking().Where(item => item.Status == FinanceStatuses.Paid || item.Status == FinanceStatuses.PartiallyRefunded).ToListAsync(cancellationToken);
        var standings = await context.MembershipStandings.AsNoTracking().ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var recent = await context.FinancialTransactions.AsNoTracking().Include(item => item.DonationCampaign).OrderByDescending(item => item.CreatedAtUtc).Take(12).ToListAsync(cancellationToken);
        return ApiResponse<FinanceDashboardDto>.SuccessResponse(new FinanceDashboardDto(
            paid.Sum(item => item.AmountCents), paid.Sum(item => item.RefundedAmountCents),
            paid.Where(item => item.Kind == FinanceKinds.Membership).Sum(item => item.AmountCents - item.RefundedAmountCents),
            paid.Where(item => item.Kind == FinanceKinds.Donation).Sum(item => item.AmountCents - item.RefundedAmountCents),
            standings.Count(item => item.CurrentPeriodEndUtc > now), standings.Count(item => item.CurrentPeriodEndUtc > now && item.CurrentPeriodEndUtc <= now.AddDays(30)),
            paid.Count, recent.Select(MapTransaction).ToList()));
    }

    public async Task<ApiResponse<IReadOnlyList<FinancialTransactionDto>>> GetTransactionsAsync(string? status, string? kind, string? search, CancellationToken cancellationToken)
    {
        var query = context.FinancialTransactions.AsNoTracking().Include(item => item.DonationCampaign).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(kind)) query = query.Where(item => item.Kind == kind);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.PayerEmail.ToLower().Contains(term) || (item.PayerName != null && item.PayerName.ToLower().Contains(term)) || item.ReceiptNumber.ToLower().Contains(term));
        }
        var items = await query.OrderByDescending(item => item.CreatedAtUtc).Take(500).ToListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<FinancialTransactionDto>>.SuccessResponse(items.Select(MapTransaction).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<AdminMembershipDto>>> GetMembershipsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = context.Users.AsNoTracking().Include(item => item.Member).Where(item => item.MemberId != null && item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Email.ToLower().Contains(term) ||
                (item.FirstName != null && item.FirstName.ToLower().Contains(term)) ||
                (item.LastName != null && item.LastName.ToLower().Contains(term)) ||
                (item.Member != null && (item.Member.FirstName.ToLower().Contains(term) || item.Member.LastName.ToLower().Contains(term))));
        }
        var users = await query.OrderBy(item => item.LastName).ThenBy(item => item.FirstName).Take(500).ToListAsync(cancellationToken);
        var ids = users.Select(item => item.Id).ToList();
        var standings = await context.MembershipStandings.AsNoTracking().Include(item => item.Plan)
            .Where(item => ids.Contains(item.UserId)).ToDictionaryAsync(item => item.UserId, cancellationToken);
        var rows = users.Select(user =>
        {
            standings.TryGetValue(user.Id, out var standing);
            var name = user.Member == null ? $"{user.FirstName} {user.LastName}" : $"{user.Member.FirstName} {user.Member.LastName}";
            return new AdminMembershipDto(user.Id, name.Trim(), user.Email, standing == null ? MembershipStatuses.Inactive : EffectiveStatus(standing),
                standing?.Plan?.Name, standing?.CurrentPeriodEndUtc, standing?.GraceEndsAtUtc, standing?.AutoRenew ?? false);
        }).ToList();
        return ApiResponse<IReadOnlyList<AdminMembershipDto>>.SuccessResponse(rows);
    }

    public async Task<ApiResponse<FinancialTransactionDto>> RefundAsync(Guid transactionId, RefundTransactionRequest request, CancellationToken cancellationToken)
    {
        var item = await context.FinancialTransactions.Include(value => value.DonationCampaign).SingleOrDefaultAsync(value => value.Id == transactionId, cancellationToken);
        if (item == null || item.Status is not (FinanceStatuses.Paid or FinanceStatuses.PartiallyRefunded)) return ApiResponse<FinancialTransactionDto>.ErrorResponse("Paid transaction not found");
        if (string.IsNullOrWhiteSpace(item.StripePaymentIntentId)) return ApiResponse<FinancialTransactionDto>.ErrorResponse("This transaction cannot be refunded automatically");
        var remaining = item.AmountCents - item.RefundedAmountCents;
        var amount = request.AmountCents ?? remaining;
        if (amount <= 0 || amount > remaining) return ApiResponse<FinancialTransactionDto>.ErrorResponse("Invalid refund amount");
        PaymentRefundResult providerRefund;
        try
        {
            providerRefund = await paymentGateway.RefundAsync(
                item.StripePaymentIntentId,
                amount == remaining ? null : amount,
                request.Reason,
                $"hcbe-refund-{item.Id:N}-{item.RefundedAmountCents}-{amount}",
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Refund failed for transaction {TransactionId}", transactionId);
            return ApiResponse<FinancialTransactionDto>.ErrorResponse("Payment provider rejected the refund");
        }

        if (!providerRefund.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
        {
            if (providerRefund.Status is "failed" or "canceled")
                return ApiResponse<FinancialTransactionDto>.ErrorResponse("Payment provider could not complete the refund");

            context.Notifications.Add(new Notification
            {
                Type = "finance-alert",
                Title = "Remboursement en traitement",
                Message = $"Le remboursement Stripe {providerRefund.RefundId} du reçu {item.ReceiptNumber} est {providerRefund.Status}.",
                Link = "/admin/finance",
                RelatedEntityId = item.Id
            });
            await context.SaveChangesAsync(cancellationToken);
            return ApiResponse<FinancialTransactionDto>.SuccessResponse(
                MapTransaction(item),
                "Refund accepted by the payment provider and awaiting confirmation");
        }

        item.RefundedAmountCents += amount;
        item.RefundedAtUtc = DateTime.UtcNow;
        item.Status = item.RefundedAmountCents >= item.AmountCents ? FinanceStatuses.Refunded : FinanceStatuses.PartiallyRefunded;
        item.UpdatedAtUtc = DateTime.UtcNow;
        if (item.Status == FinanceStatuses.Refunded && item.Kind == FinanceKinds.Membership && item.UserId is Guid refundedUserId)
        {
            var standing = await context.MembershipStandings.SingleOrDefaultAsync(value => value.UserId == refundedUserId && value.LastTransactionId == item.Id, cancellationToken);
            if (standing != null)
            {
                if (!string.IsNullOrWhiteSpace(standing.StripeSubscriptionId))
                {
                    try
                    {
                        await paymentGateway.CancelSubscriptionAsync(standing.StripeSubscriptionId, cancellationToken);
                        standing.AutoRenew = false;
                        standing.StripeSubscriptionId = null;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Membership refund succeeded but subscription cancellation failed for {TransactionId}", item.Id);
                        context.Notifications.Add(new Notification
                        {
                            Type = "finance-alert",
                            Title = "Abonnement à annuler dans Stripe",
                            Message = $"Le reçu {item.ReceiptNumber} a été remboursé, mais son abonnement doit être annulé manuellement.",
                            Link = "/admin/finance",
                            RelatedEntityId = item.Id
                        });
                    }
                }
                else
                {
                    standing.AutoRenew = false;
                }
                ResetToCommunityMembership(standing);
            }
        }
        await context.SaveChangesAsync(cancellationToken);
        return ApiResponse<FinancialTransactionDto>.SuccessResponse(MapTransaction(item));
    }

    public async Task<ApiResponse<MembershipStandingDto>> UpdateMembershipAsync(Guid userId, UpdateMembershipStandingRequest request, CancellationToken cancellationToken)
    {
        var user = await context.Users.Include(item => item.Member).SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user == null) return ApiResponse<MembershipStandingDto>.ErrorResponse("Account not found");
        if (!new[] { MembershipStatuses.Active, MembershipStatuses.Inactive, MembershipStatuses.GracePeriod, MembershipStatuses.Expired }.Contains(request.Status)) return ApiResponse<MembershipStandingDto>.ErrorResponse("Invalid membership status");
        var standing = await GetOrCreateStandingAsync(userId, cancellationToken);
        standing.Status = request.Status;
        standing.CurrentPeriodEndUtc = request.CurrentPeriodEndUtc;
        standing.GraceEndsAtUtc = request.Status == MembershipStatuses.GracePeriod ? request.CurrentPeriodEndUtc?.AddDays(options.MembershipGracePeriodDays) : null;
        standing.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return ApiResponse<MembershipStandingDto>.SuccessResponse(MapStanding(standing, user, standing.Plan));
    }

    public async Task<ApiResponse<MembershipVerificationDto>> VerifyMembershipAsync(string code, CancellationToken cancellationToken)
    {
        if (!TryDecodeVerificationCode(code, out var userId)) return ApiResponse<MembershipVerificationDto>.SuccessResponse(new(false, MembershipStatuses.Inactive, "", null, null, null, code));
        var standing = await context.MembershipStandings.AsNoTracking().Include(item => item.User).ThenInclude(item => item!.Member).Include(item => item.Plan).SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (standing?.User == null) return ApiResponse<MembershipVerificationDto>.SuccessResponse(new(false, MembershipStatuses.Inactive, "", null, null, null, code));
        var effective = EffectiveStatus(standing);
        var name = standing.User.Member != null ? $"{standing.User.Member.FirstName} {standing.User.Member.LastName}" : $"{standing.User.FirstName} {standing.User.LastName}";
        return ApiResponse<MembershipVerificationDto>.SuccessResponse(new(effective is MembershipStatuses.Active or MembershipStatuses.GracePeriod, effective, name.Trim(), standing.Plan?.Name, standing.Plan?.NameEn, standing.CurrentPeriodEndUtc, code));
    }

    public Task<FinancialTransaction?> FindReceiptAsync(string token, CancellationToken cancellationToken) =>
        context.FinancialTransactions.AsNoTracking().Include(item => item.MembershipPlan).Include(item => item.DonationCampaign)
            .SingleOrDefaultAsync(item => item.ReceiptToken == token && item.PaidAtUtc != null, cancellationToken);

    public async Task<int> ProcessMembershipRemindersAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var candidates = await context.MembershipStandings.Include(item => item.User).ThenInclude(item => item!.Member).Include(item => item.Plan)
            .Where(item => item.CurrentPeriodEndUtc != null && item.CurrentPeriodEndUtc <= now.AddDays(30) && item.CurrentPeriodEndUtc >= now.AddDays(-options.MembershipGracePeriodDays))
            .ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var standing in candidates)
        {
            if (standing.User == null) continue;
            RefreshStandingStatus(standing);
            var days = (int)Math.Ceiling((standing.CurrentPeriodEndUtc!.Value - now).TotalDays);
            var key = days <= 0 ? "expired" : days <= 7 ? "7-days" : "30-days";
            if (standing.LastReminderKey == key) continue;
            var email = emailRenderer.MembershipReminder(standing.User.FirstName ?? standing.User.Member?.FirstName, standing.CurrentPeriodEndUtc.Value, $"{PublicAppUrl}/espace-membre?section=membership", days <= 0);
            emailOutbox.Enqueue(standing.User.Email, email.Subject, email.HtmlBody, "MembershipStanding", standing.Id);
            context.Notifications.Add(new Notification { UserId = standing.UserId, Type = "membership", Title = days <= 0 ? "Adhésion à renouveler" : "Renouvellement à venir", Message = days <= 0 ? "Votre période d’adhésion est terminée. Renouvelez-la depuis votre espace membre." : $"Votre adhésion arrive à échéance dans {Math.Max(1, days)} jours.", Link = "/espace-membre?section=membership", RelatedEntityId = standing.Id });
            standing.LastReminderKey = key; standing.LastReminderAtUtc = now; sent++;
        }
        if (sent > 0) await context.SaveChangesAsync(cancellationToken);
        return sent;
    }

    private async Task<ApiResponse<CheckoutSessionDto>> StartCheckoutAsync(FinancialTransaction transaction, string productName, string? priceId, string? customerId, CancellationToken cancellationToken)
    {
        if (!paymentGateway.IsEnabled)
        {
            transaction.Status = FinanceStatuses.Failed; transaction.FailureReason = "Payments are not configured"; transaction.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return ApiResponse<CheckoutSessionDto>.ErrorResponse("Online payments are temporarily unavailable");
        }
        try
        {
            var result = await paymentGateway.CreateCheckoutAsync(new PaymentCheckoutRequest(transaction.Id, transaction.Kind,
                transaction.AmountCents, transaction.Currency, productName, priceId, transaction.PayerEmail, transaction.UserId,
                customerId, transaction.IsRecurring, $"{PublicAppUrl}/paiement/merci?session_id={{CHECKOUT_SESSION_ID}}",
                transaction.Kind == FinanceKinds.Membership ? $"{PublicAppUrl}/espace-membre?section=membership&payment=cancelled" : $"{PublicAppUrl}/contribuer?payment=cancelled"), cancellationToken);
            transaction.StripeCheckoutSessionId = result.SessionId;
            transaction.StripeCustomerId = result.CustomerId;
            transaction.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return ApiResponse<CheckoutSessionDto>.SuccessResponse(new(transaction.Id, result.Url, result.SessionId));
        }
        catch (Exception exception)
        {
            transaction.Status = FinanceStatuses.Failed; transaction.FailureReason = "Checkout creation failed"; transaction.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogError(exception, "Checkout creation failed for transaction {TransactionId}", transaction.Id);
            return ApiResponse<CheckoutSessionDto>.ErrorResponse("Unable to start secure checkout");
        }
    }

    private async Task ApplyWebhookAsync(string eventType, JsonElement root, CancellationToken cancellationToken)
    {
        if (eventType is "checkout.session.completed" or "checkout.session.async_payment_succeeded")
        {
            var id = Nested(root, "metadata", "hcbe_transaction_id");
            if (!Guid.TryParse(id, out var transactionId)) return;
            var transaction = await context.FinancialTransactions.SingleOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
            if (transaction == null || transaction.Status is FinanceStatuses.Paid or FinanceStatuses.PartiallyRefunded or FinanceStatuses.Refunded or FinanceStatuses.Disputed) return;
            if (eventType == "checkout.session.completed" &&
                String(root, "payment_status") is not ("paid" or "no_payment_required"))
            {
                // Delayed payment methods complete Checkout before funds settle. Keep
                // the ledger pending until async_payment_succeeded is received.
                transaction.StripeCheckoutSessionId = String(root, "id") ?? transaction.StripeCheckoutSessionId;
                transaction.StripeCustomerId = Id(root, "customer") ?? transaction.StripeCustomerId;
                transaction.StripeSubscriptionId = Id(root, "subscription") ?? transaction.StripeSubscriptionId;
                transaction.UpdatedAtUtc = DateTime.UtcNow;
                return;
            }
            transaction.StripeCheckoutSessionId = String(root, "id") ?? transaction.StripeCheckoutSessionId;
            transaction.StripePaymentIntentId = Id(root, "payment_intent");
            transaction.StripeCustomerId = Id(root, "customer") ?? transaction.StripeCustomerId;
            transaction.StripeSubscriptionId = Id(root, "subscription");
            transaction.StripeInvoiceId = Id(root, "invoice");
            transaction.AmountCents = Long(root, "amount_total") ?? transaction.AmountCents;
            transaction.Status = FinanceStatuses.Paid;
            transaction.PaidAtUtc = DateTime.UtcNow;
            transaction.UpdatedAtUtc = DateTime.UtcNow;
            await CompletePaidTransactionAsync(transaction, cancellationToken);
            return;
        }
        if (eventType is "checkout.session.expired" or "checkout.session.async_payment_failed")
        {
            var id = Nested(root, "metadata", "hcbe_transaction_id");
            if (Guid.TryParse(id, out var transactionId))
            {
                var transaction = await context.FinancialTransactions.SingleOrDefaultAsync(item => item.Id == transactionId && item.Status == FinanceStatuses.Pending, cancellationToken);
                if (transaction != null) { transaction.Status = FinanceStatuses.Failed; transaction.FailureReason = eventType; transaction.UpdatedAtUtc = DateTime.UtcNow; }
            }
            return;
        }
        if (eventType is "invoice.paid" or "invoice.payment_failed")
        {
            var subscriptionId = Id(root, "subscription") ?? NestedId(root, "parent", "subscription_details", "subscription");
            if (string.IsNullOrWhiteSpace(subscriptionId)) return;
            var standing = await context.MembershipStandings.Include(item => item.User).SingleOrDefaultAsync(item => item.StripeSubscriptionId == subscriptionId, cancellationToken);
            var original = await context.FinancialTransactions.OrderBy(item => item.CreatedAtUtc).FirstOrDefaultAsync(item => item.StripeSubscriptionId == subscriptionId, cancellationToken);
            if (eventType == "invoice.payment_failed")
            {
                if (standing != null)
                {
                    var graceStart = standing.CurrentPeriodEndUtc > DateTime.UtcNow ? standing.CurrentPeriodEndUtc.Value : DateTime.UtcNow;
                    standing.GraceEndsAtUtc = graceStart.AddDays(options.MembershipGracePeriodDays);
                    RefreshStandingStatus(standing);
                }
                if (standing?.User != null) context.Notifications.Add(new Notification { UserId = standing.UserId, Type = "payment", Title = "Paiement non complété", Message = "Le renouvellement automatique de votre adhésion a échoué. Vérifiez votre mode de paiement.", Link = "/espace-membre?section=membership" });
                context.Notifications.Add(new Notification { Type = "finance-alert", Title = "Échec de paiement récurrent", Message = $"Le paiement de l’abonnement {subscriptionId} a échoué.", Link = "/admin/finance" });
                return;
            }
            var invoiceId = String(root, "id");
            if (invoiceId == null || await context.FinancialTransactions.AnyAsync(item => item.StripeInvoiceId == invoiceId, cancellationToken)) return;
            if (original == null) return;
            var renewal = NewTransaction(original.Kind, Long(root, "amount_paid") ?? original.AmountCents, String(root, "currency") ?? original.Currency, original.PayerEmail, original.PayerName, original.UserId);
            renewal.MembershipPlanId = original.MembershipPlanId; renewal.DonationCampaignId = original.DonationCampaignId;
            renewal.IsAnonymous = original.IsAnonymous; renewal.AllowPublicRecognition = original.AllowPublicRecognition; renewal.IsRecurring = true;
            renewal.StripeSubscriptionId = subscriptionId; renewal.StripeCustomerId = Id(root, "customer"); renewal.StripePaymentIntentId = Id(root, "payment_intent"); renewal.StripeInvoiceId = invoiceId;
            renewal.Status = FinanceStatuses.Paid; renewal.PaidAtUtc = DateTime.UtcNow;
            context.FinancialTransactions.Add(renewal);
            await CompletePaidTransactionAsync(renewal, cancellationToken);
            return;
        }
        if (eventType is "customer.subscription.updated" or "customer.subscription.deleted")
        {
            var subscriptionId = String(root, "id");
            var standing = await context.MembershipStandings.SingleOrDefaultAsync(item => item.StripeSubscriptionId == subscriptionId, cancellationToken);
            if (standing != null)
            {
                if (eventType == "customer.subscription.deleted")
                {
                    standing.AutoRenew = false;
                    standing.StripeSubscriptionId = null;
                }
                else
                {
                    var status = String(root, "status");
                    standing.AutoRenew = status is "active" or "trialing" && Bool(root, "cancel_at_period_end") != true;
                }
                RefreshStandingStatus(standing);
            }
            return;
        }
        if (eventType is "charge.refunded" or "charge.dispute.created")
        {
            var intentId = Id(root, "payment_intent");
            var transaction = await context.FinancialTransactions.SingleOrDefaultAsync(item => item.StripePaymentIntentId == intentId, cancellationToken);
            if (transaction == null) return;
            if (eventType == "charge.dispute.created")
            {
                transaction.Status = FinanceStatuses.Disputed;
                context.Notifications.Add(new Notification { Type = "finance-alert", Title = "Litige de paiement", Message = $"Le reçu {transaction.ReceiptNumber} fait l’objet d’un litige Stripe.", Link = "/admin/finance", RelatedEntityId = transaction.Id });
            }
            else
            {
                transaction.RefundedAmountCents = Long(root, "amount_refunded") ?? transaction.AmountCents;
                transaction.Status = transaction.RefundedAmountCents >= transaction.AmountCents ? FinanceStatuses.Refunded : FinanceStatuses.PartiallyRefunded;
                transaction.RefundedAtUtc = DateTime.UtcNow;
                if (transaction.Status == FinanceStatuses.Refunded && transaction.Kind == FinanceKinds.Membership && transaction.UserId is Guid refundedUserId)
                {
                    var standing = await context.MembershipStandings.SingleOrDefaultAsync(item => item.UserId == refundedUserId && item.LastTransactionId == transaction.Id, cancellationToken);
                    if (standing != null) ResetToCommunityMembership(standing);
                }
            }
            transaction.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private async Task CompletePaidTransactionAsync(FinancialTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.Kind == FinanceKinds.Membership && transaction.UserId is Guid userId)
        {
            var standing = await GetOrCreateStandingAsync(userId, cancellationToken);
            var start = standing.CurrentPeriodEndUtc > DateTime.UtcNow ? standing.CurrentPeriodEndUtc.Value : DateTime.UtcNow;
            standing.PlanId = transaction.MembershipPlanId;
            standing.Status = MembershipStatuses.Active;
            standing.CurrentPeriodStartUtc = DateTime.UtcNow;
            standing.CurrentPeriodEndUtc = start.AddYears(1);
            standing.GraceEndsAtUtc = standing.CurrentPeriodEndUtc.Value.AddDays(options.MembershipGracePeriodDays);
            standing.AutoRenew = transaction.IsRecurring;
            standing.StripeCustomerId = transaction.StripeCustomerId ?? standing.StripeCustomerId;
            standing.StripeSubscriptionId = transaction.StripeSubscriptionId ?? standing.StripeSubscriptionId;
            standing.LastTransactionId = transaction.Id;
            standing.LastReminderKey = null;
            standing.UpdatedAtUtc = DateTime.UtcNow;
        }
        var email = emailRenderer.PaymentReceipt(transaction.PayerName, transaction.Kind, transaction.AmountCents, transaction.Currency, transaction.ReceiptNumber, $"{PublicApiUrl}/api/finance/receipts/{transaction.ReceiptToken}");
        emailOutbox.Enqueue(transaction.PayerEmail, email.Subject, email.HtmlBody, "FinancialTransaction", transaction.Id);
        if (transaction.UserId is Guid recipientId)
            context.Notifications.Add(new Notification { UserId = recipientId, Type = "payment", Title = transaction.Kind == FinanceKinds.Membership ? "Adhésion confirmée" : "Contribution reçue", Message = $"Votre paiement de {transaction.AmountCents / 100m:0.00} {transaction.Currency.ToUpperInvariant()} a été confirmé.", Link = "/espace-membre?section=membership", RelatedEntityId = transaction.Id });
    }

    private async Task<MembershipStanding> GetOrCreateStandingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var standing = await context.MembershipStandings.Include(item => item.Plan).SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (standing != null) return standing;
        standing = CommunityMembership.CreateStanding(userId, DateTime.UtcNow);
        context.MembershipStandings.Add(standing);
        standing.Plan = await context.MembershipPlans.SingleOrDefaultAsync(item => item.Id == CommunityMembership.PlanId, cancellationToken);
        return standing;
    }

    private void RefreshStandingStatus(MembershipStanding standing)
    {
        standing.Status = EffectiveStatus(standing);
        standing.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string EffectiveStatus(MembershipStanding standing)
    {
        var now = DateTime.UtcNow;
        if (standing.CurrentPeriodEndUtc == null) return standing.Status == MembershipStatuses.Inactive ? MembershipStatuses.Inactive : MembershipStatuses.Expired;
        if (standing.CurrentPeriodEndUtc > now) return MembershipStatuses.Active;
        if (standing.GraceEndsAtUtc > now) return MembershipStatuses.GracePeriod;
        return MembershipStatuses.Expired;
    }

    private void ResetToCommunityMembership(MembershipStanding standing)
    {
        var now = DateTime.UtcNow;
        standing.PlanId = CommunityMembership.PlanId;
        standing.Status = MembershipStatuses.Active;
        standing.CurrentPeriodStartUtc = now;
        standing.CurrentPeriodEndUtc = now.AddYears(1);
        standing.GraceEndsAtUtc = now.AddYears(1).AddDays(options.MembershipGracePeriodDays);
        standing.AutoRenew = false;
        standing.StripeSubscriptionId = null;
        standing.LastReminderKey = null;
        standing.UpdatedAtUtc = now;
    }

    private MembershipStandingDto MapStanding(MembershipStanding item, User user, MembershipPlan? plan)
    {
        var code = EffectiveStatus(item) is MembershipStatuses.Active or MembershipStatuses.GracePeriod ? EncodeVerificationCode(user.Id) : null;
        return new(EffectiveStatus(item), item.CurrentPeriodStartUtc, item.CurrentPeriodEndUtc, item.GraceEndsAtUtc, item.AutoRenew,
            !string.IsNullOrWhiteSpace(item.StripeCustomerId), !string.IsNullOrWhiteSpace(item.StripeSubscriptionId),
            plan == null ? null : MapPlan(plan), code, code == null ? null : $"{PublicAppUrl}/adhesion/verifier/{code}");
    }

    private static FinancialTransaction NewTransaction(string kind, long amount, string currency, string email, string? name, Guid? userId) => new()
    {
        Kind = kind, AmountCents = amount, Currency = NormalizeCurrency(currency), PayerEmail = email.Trim().ToLowerInvariant(),
        PayerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim(), UserId = userId,
        ReceiptNumber = $"HCBE-{DateTime.UtcNow:yyyy}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}",
        ReceiptToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()
    };

    private FinancialTransactionDto MapTransaction(FinancialTransaction item) => new(item.Id, item.Kind, item.Status, item.AmountCents,
        item.RefundedAmountCents, item.Currency, item.PayerEmail, item.PayerName, item.IsAnonymous, item.AllowPublicRecognition,
        item.IsRecurring, item.ReceiptNumber, item.PaidAtUtc == null ? null : $"{PublicApiUrl}/api/finance/receipts/{item.ReceiptToken}",
        item.MembershipPlanId, item.DonationCampaignId, item.DonationCampaign?.Title, item.CreatedAtUtc, item.PaidAtUtc, item.RefundedAtUtc);

    private static MembershipPlanDto MapPlan(MembershipPlan item) => new(item.Id, item.Name, item.NameEn, item.Description,
        item.DescriptionEn, item.AmountCents, item.Currency, item.BillingMode, item.StripePriceId, DeserializeBenefits(item.BenefitsJson), item.IsActive, item.DisplayOrder);
    private static DonationCampaignDto MapCampaign(DonationCampaign item, long raised, int count) => new(item.Id, item.Slug, item.Title,
        item.TitleEn, item.Description, item.DescriptionEn, item.GoalAmountCents, raised, item.Currency, item.ImageUrl,
        item.AllowRecurring, item.IsPublished, item.StartsAtUtc, item.EndsAtUtc, count);

    private static void ApplyPlan(MembershipPlan item, UpsertMembershipPlanRequest request)
    {
        item.Name = request.Name.Trim(); item.NameEn = Clean(request.NameEn); item.Description = request.Description.Trim(); item.DescriptionEn = Clean(request.DescriptionEn);
        item.AmountCents = request.AmountCents; item.Currency = NormalizeCurrency(request.Currency); item.BillingMode = request.BillingMode;
        item.StripePriceId = Clean(request.StripePriceId); item.BenefitsJson = JsonSerializer.Serialize(request.Benefits?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Take(12).ToList() ?? []);
        item.IsActive = request.IsActive; item.DisplayOrder = request.DisplayOrder; item.UpdatedAtUtc = DateTime.UtcNow;
    }
    private static string? ValidatePlan(UpsertMembershipPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 160) return "A valid plan name is required";
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 4000) return "A valid plan description is required";
        if (request.BillingMode == CommunityMembership.BillingMode && request.AmountCents != 0) return "The community membership must remain free";
        if (request.BillingMode != CommunityMembership.BillingMode && (request.AmountCents < 100 || request.AmountCents > 1_000_000)) return "Plan amount is outside the accepted range";
        if (NormalizeCurrency(request.Currency) != "cad") return "Only CAD membership plans are supported";
        if (request.BillingMode is not ("Free" or "Annual" or "Recurring")) return "Billing mode must be Free, Annual or Recurring";
        if (request.BillingMode == CommunityMembership.BillingMode && !string.IsNullOrWhiteSpace(request.StripePriceId)) return "The free community membership cannot have a Stripe price";
        if (!string.IsNullOrWhiteSpace(request.StripePriceId) &&
            (request.StripePriceId.Length > 255 || !request.StripePriceId.StartsWith("price_", StringComparison.Ordinal)))
            return "Stripe price ID must start with price_";
        return null;
    }
    private async Task<string?> ValidateCampaignAsync(UpsertDonationCampaignRequest request, Guid? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 180 || string.IsNullOrWhiteSpace(request.Slug) || request.Slug.Length > 120) return "Campaign title and slug are required";
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 8000) return "A valid campaign description is required";
        if (!request.Slug.All(value => char.IsLetterOrDigit(value) || value == '-')) return "Campaign slug may only contain letters, numbers, and hyphens";
        if (request.GoalAmountCents < 0 || request.GoalAmountCents > 1_000_000_000) return "Invalid campaign goal";
        if (NormalizeCurrency(request.Currency) != "cad") return "Only CAD donation campaigns are supported";
        if (request.ImageUrl?.Length > 2048) return "Campaign image URL is too long";
        if (!string.IsNullOrWhiteSpace(request.ImageUrl) &&
            (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var imageUri) || imageUri.Scheme != Uri.UriSchemeHttps))
            return "Campaign image must be a valid HTTPS URL";
        if (request.EndsAtUtc != null && request.StartsAtUtc != null && request.EndsAtUtc <= request.StartsAtUtc) return "Campaign end must follow its start";
        if (await context.DonationCampaigns.AnyAsync(item => item.Slug == request.Slug.Trim().ToLower() && item.Id != id, cancellationToken)) return "Campaign slug is already used";
        return null;
    }
    private static void ApplyCampaign(DonationCampaign item, UpsertDonationCampaignRequest request)
    {
        item.Slug = request.Slug.Trim().ToLowerInvariant(); item.Title = request.Title.Trim(); item.TitleEn = Clean(request.TitleEn);
        item.Description = request.Description.Trim(); item.DescriptionEn = Clean(request.DescriptionEn); item.GoalAmountCents = request.GoalAmountCents;
        item.Currency = NormalizeCurrency(request.Currency); item.ImageUrl = Clean(request.ImageUrl); item.AllowRecurring = request.AllowRecurring;
        item.IsPublished = request.IsPublished; item.StartsAtUtc = request.StartsAtUtc?.ToUniversalTime(); item.EndsAtUtc = request.EndsAtUtc?.ToUniversalTime(); item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private string EncodeVerificationCode(Guid userId)
    {
        var secret = configuration["JwtSettings:Secret"] ?? "hcbe";
        var signature = Convert.ToHexString(HMACSHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret), userId.ToByteArray()))[..12];
        return $"{userId:N}{signature}".ToLowerInvariant();
    }
    private bool TryDecodeVerificationCode(string code, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(code) || code.Length != 44 || !Guid.TryParseExact(code[..32], "N", out userId)) return false;
        return CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(code), System.Text.Encoding.ASCII.GetBytes(EncodeVerificationCode(userId)));
    }
    private static IReadOnlyList<string> DeserializeBenefits(string json) { try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; } catch { return []; } }
    private static string NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? "cad" : value.Trim().ToLowerInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? String(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static long? Long(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.TryGetInt64(out var number) ? number : null;
    private static bool? Bool(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
    private static string? Id(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return null;
        if (value.ValueKind == JsonValueKind.String) return value.GetString();
        return value.ValueKind == JsonValueKind.Object ? String(value, "id") : null;
    }
    private static string? Nested(JsonElement element, string parent, string property) => element.TryGetProperty(parent, out var value) && value.ValueKind == JsonValueKind.Object ? String(value, property) : null;
    private static string? NestedId(JsonElement element, string first, string second, string property) => element.TryGetProperty(first, out var one) && one.ValueKind == JsonValueKind.Object && one.TryGetProperty(second, out var two) && two.ValueKind == JsonValueKind.Object ? Id(two, property) : null;
}
