using System.Security.Cryptography;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class EventCommerceService(
    ApplicationDbContext context,
    IPaymentGateway paymentGateway,
    IEmailOutbox emailOutbox,
    IEmailTemplateRenderer emailRenderer,
    IConfiguration configuration,
    ILogger<EventCommerceService> logger) : IEventCommerceService
{
    private static readonly string[] PublicStatuses = ["Active", "À venir", "En cours", "Upcoming", "Ongoing", "Published"];
    private string PublicAppUrl => (configuration["PublicAppUrl"] ?? "https://hcbe.ca").TrimEnd('/');
    private string PublicApiUrl => (configuration["PublicApiUrl"] ?? PublicAppUrl).TrimEnd('/');

    public async Task<ApiResponse<IReadOnlyList<TicketTierDto>>> GetTiersAsync(Guid eventId, bool admin, CancellationToken ct)
    {
        var exists = await context.Events.AsNoTracking().AnyAsync(item => item.Id == eventId && (admin || item.TicketingEnabled), ct);
        if (!exists) return ApiResponse<IReadOnlyList<TicketTierDto>>.ErrorResponse("Event not found");
        var tiers = await context.EventTicketTiers.AsNoTracking().Where(item => item.EventId == eventId && (admin || item.IsActive))
            .OrderBy(item => item.DisplayOrder).ThenBy(item => item.PriceCents).ToListAsync(ct);
        var availability = await AvailabilityAsync(eventId, ct);
        return ApiResponse<IReadOnlyList<TicketTierDto>>.SuccessResponse(tiers.Select(item => MapTier(item, availability)).ToList());
    }

    public async Task<ApiResponse<TicketTierDto>> CreateTierAsync(Guid eventId, UpsertTicketTierRequest request, CancellationToken ct)
    {
        if (!await context.Events.AnyAsync(item => item.Id == eventId, ct)) return ApiResponse<TicketTierDto>.ErrorResponse("Event not found");
        var error = ValidateTier(request); if (error != null) return ApiResponse<TicketTierDto>.ErrorResponse(error);
        var item = new EventTicketTier { EventId = eventId }; Apply(item, request);
        context.EventTicketTiers.Add(item);
        var eventEntity = await context.Events.SingleAsync(value => value.Id == eventId, ct);
        eventEntity.TicketingEnabled = true; eventEntity.RegistrationMode = "Disabled"; eventEntity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return ApiResponse<TicketTierDto>.SuccessResponse(MapTier(item, new Dictionary<Guid, (int Sold, int Reserved)>()));
    }

    public async Task<ApiResponse<TicketTierDto>> UpdateTierAsync(Guid eventId, Guid tierId, UpsertTicketTierRequest request, CancellationToken ct)
    {
        var error = ValidateTier(request); if (error != null) return ApiResponse<TicketTierDto>.ErrorResponse(error);
        var item = await context.EventTicketTiers.SingleOrDefaultAsync(value => value.Id == tierId && value.EventId == eventId, ct);
        if (item == null) return ApiResponse<TicketTierDto>.ErrorResponse("Ticket tier not found");
        var issued = await context.EventTickets.CountAsync(value => value.TierId == tierId && value.Status != "Refunded" && value.Status != "Cancelled", ct);
        if (request.Quantity < issued) return ApiResponse<TicketTierDto>.ErrorResponse("Quantity cannot be lower than tickets already issued");
        Apply(item, request); await context.SaveChangesAsync(ct);
        return ApiResponse<TicketTierDto>.SuccessResponse(MapTier(item, await AvailabilityAsync(eventId, ct)));
    }

    public async Task<ApiResponse> DeleteTierAsync(Guid eventId, Guid tierId, CancellationToken ct)
    {
        var item = await context.EventTicketTiers.SingleOrDefaultAsync(value => value.Id == tierId && value.EventId == eventId, ct);
        if (item == null) return ApiResponse.CreateError("Ticket tier not found");
        if (await context.EventTicketOrderItems.AnyAsync(value => value.TierId == tierId, ct))
        { item.IsActive = false; item.UpdatedAtUtc = DateTime.UtcNow; }
        else context.EventTicketTiers.Remove(item);
        await context.SaveChangesAsync(ct); return ApiResponse.CreateSuccess("Ticket tier removed");
    }

    public async Task<ApiResponse<IReadOnlyList<PromoCodeDto>>> GetPromoCodesAsync(Guid eventId, CancellationToken ct)
    {
        var items = await context.EventPromoCodes.AsNoTracking().Where(item => item.EventId == eventId).OrderBy(item => item.Code).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<PromoCodeDto>>.SuccessResponse(items.Select(MapPromo).ToList());
    }

    public async Task<ApiResponse<PromoCodeDto>> CreatePromoCodeAsync(Guid eventId, UpsertPromoCodeRequest request, CancellationToken ct)
    {
        if (!await context.Events.AnyAsync(item => item.Id == eventId, ct)) return ApiResponse<PromoCodeDto>.ErrorResponse("Event not found");
        if (request.PercentOff <= 0 && request.AmountOffCents is not > 0) return ApiResponse<PromoCodeDto>.ErrorResponse("A discount is required");
        if (request.PercentOff > 0 && request.AmountOffCents is > 0) return ApiResponse<PromoCodeDto>.ErrorResponse("Use either a percentage or a fixed discount");
        if (request.EndsAtUtc <= request.StartsAtUtc) return ApiResponse<PromoCodeDto>.ErrorResponse("Discount end must be after its start");
        var code = NormalizeCode(request.Code);
        if (await context.EventPromoCodes.AnyAsync(item => item.EventId == eventId && item.Code == code, ct)) return ApiResponse<PromoCodeDto>.ErrorResponse("Promo code already exists");
        var item = new EventPromoCode
        {
            EventId = eventId,
            Code = code,
            PercentOff = request.PercentOff,
            AmountOffCents = request.AmountOffCents,
            MaxRedemptions = request.MaxRedemptions,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            IsActive = request.IsActive
        };
        context.EventPromoCodes.Add(item); await context.SaveChangesAsync(ct);
        return ApiResponse<PromoCodeDto>.SuccessResponse(MapPromo(item));
    }

    public async Task<ApiResponse> DeletePromoCodeAsync(Guid eventId, Guid promoId, CancellationToken ct)
    {
        var item = await context.EventPromoCodes.SingleOrDefaultAsync(value => value.Id == promoId && value.EventId == eventId, ct);
        if (item == null) return ApiResponse.CreateError("Promo code not found");
        item.IsActive = false; await context.SaveChangesAsync(ct); return ApiResponse.CreateSuccess("Promo code disabled");
    }

    public async Task<ApiResponse<TicketCheckoutDto>> CreateCheckoutAsync(Guid? userId, Guid eventId, CreateTicketCheckoutRequest request, CancellationToken ct)
    {
        var eventEntity = await context.Events.Include(item => item.CommunityOrganizer).SingleOrDefaultAsync(item => item.Id == eventId, ct);
        if (eventEntity == null || !eventEntity.TicketingEnabled || !PublicStatuses.Contains(eventEntity.Status)) return ApiResponse<TicketCheckoutDto>.ErrorResponse("Ticket sales are unavailable");
        var now = DateTime.UtcNow;
        if (eventEntity.Date <= now || eventEntity.RegistrationDeadline <= now) return ApiResponse<TicketCheckoutDto>.ErrorResponse("Ticket sales are closed");
        if (request.Items.Count == 0 || request.Items.Count > 12 || request.Items.Sum(item => item.Quantity) > 50) return ApiResponse<TicketCheckoutDto>.ErrorResponse("Select between 1 and 50 tickets");
        var selections = request.Items.GroupBy(item => item.TierId).ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var tiers = await context.EventTicketTiers.Where(item => item.EventId == eventId && selections.Keys.Contains(item.Id)).ToListAsync(ct);
        if (tiers.Count != selections.Count) return ApiResponse<TicketCheckoutDto>.ErrorResponse("A selected ticket tier is unavailable");
        await using var inventoryTransaction = context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct)
            : null;
        if (inventoryTransaction != null)
        {
            // Lock every tier for this event in a stable order. Concurrent checkouts
            // then recalculate availability one at a time and cannot oversell.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"EventTicketTiers\" WHERE \"EventId\" = {eventId} ORDER BY \"Id\" FOR UPDATE", ct);
        }
        var availability = await AvailabilityAsync(eventId, ct);
        foreach (var tier in tiers)
        {
            var quantity = selections[tier.Id];
            var state = availability.GetValueOrDefault(tier.Id);
            if (!tier.IsActive || tier.SalesStartUtc > now || tier.SalesEndUtc < now || quantity > tier.MaxPerOrder || quantity > Math.Max(0, tier.Quantity - state.Sold - state.Reserved))
                return ApiResponse<TicketCheckoutDto>.ErrorResponse($"Not enough {tier.Name} tickets are available");
        }
        var currencies = tiers.Select(item => item.Currency.ToLowerInvariant()).Distinct().ToList();
        if (currencies.Count != 1) return ApiResponse<TicketCheckoutDto>.ErrorResponse("All ticket tiers must use the same currency");
        if (eventEntity.SalesModel == "Community" && (eventEntity.CommunityOrganizer?.Status != OrganizerStatuses.Approved || !eventEntity.CommunityOrganizer.StripeChargesEnabled || string.IsNullOrWhiteSpace(eventEntity.CommunityOrganizer.StripeAccountId)))
            return ApiResponse<TicketCheckoutDto>.ErrorResponse("Organizer payments are not ready");

        EventPromoCode? promo = null;
        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var code = NormalizeCode(request.PromoCode);
            promo = await context.EventPromoCodes.SingleOrDefaultAsync(item => item.EventId == eventId && item.Code == code && item.IsActive, ct);
            if (promo == null || promo.StartsAtUtc > now || promo.EndsAtUtc < now || promo.MaxRedemptions <= promo.RedemptionCount)
                return ApiResponse<TicketCheckoutDto>.ErrorResponse("Promo code is invalid or expired");
        }

        var subtotal = tiers.Sum(tier => tier.PriceCents * selections[tier.Id]);
        var discount = promo == null ? 0 : promo.PercentOff > 0 ? subtotal * promo.PercentOff / 100 : Math.Min(subtotal, promo.AmountOffCents ?? 0);
        var total = Math.Max(0, subtotal - discount);
        var fee = eventEntity.SalesModel == "Community" ? total * Math.Clamp(eventEntity.PlatformFeePercent, 0, 25) / 100 : 0;
        var order = new EventTicketOrder
        {
            EventId = eventId,
            UserId = userId,
            BuyerName = request.BuyerName.Trim(),
            BuyerEmail = request.BuyerEmail.Trim().ToLowerInvariant(),
            Currency = currencies[0],
            SubtotalCents = subtotal,
            DiscountCents = discount,
            TotalCents = total,
            PlatformFeeCents = fee,
            PromoCodeId = promo?.Id,
            OrderNumber = OrderNumber(),
            AccessToken = Token(),
            // Stripe Checkout requires at least 30 minutes. A small safety margin
            // avoids clock/network skew while keeping the inventory hold short.
            ExpiresAtUtc = now.AddMinutes(35),
            StripeAccountId = eventEntity.CommunityOrganizer?.StripeAccountId
        };
        foreach (var tier in tiers) order.Items.Add(new EventTicketOrderItem
        {
            TierId = tier.Id,
            TierName = tier.Name,
            TierNameEn = tier.NameEn,
            Quantity = selections[tier.Id],
            UnitPriceCents = tier.PriceCents,
            LineTotalCents = tier.PriceCents * selections[tier.Id]
        });
        context.EventTicketOrders.Add(order);

        if (total == 0)
        {
            CompleteFreeOrder(order, eventEntity); if (promo != null) promo.RedemptionCount++;
            QueueTicketEmail(order, eventEntity); await context.SaveChangesAsync(ct);
            if (inventoryTransaction != null) await inventoryTransaction.CommitAsync(ct);
            return ApiResponse<TicketCheckoutDto>.SuccessResponse(new(order.Id, order.Status, null, "free", order.OrderNumber, order.AccessToken, TicketPdfUrl(order)));
        }
        await context.SaveChangesAsync(ct);
        if (inventoryTransaction != null) await inventoryTransaction.CommitAsync(ct);
        if (!paymentGateway.IsEnabled) return await FailCheckout(order, "Online payments are temporarily unavailable", ct);
        try
        {
            var result = await paymentGateway.CreateCheckoutAsync(new PaymentCheckoutRequest(order.Id, "Ticket", total, order.Currency,
                $"Billets — {eventEntity.Title}", null, order.BuyerEmail, userId, null, false,
                $"{PublicAppUrl}/billets/commande/{order.AccessToken}?session_id={{CHECKOUT_SESSION_ID}}",
                $"{PublicAppUrl}/actualites/evenements/{eventId}?payment=cancelled",
                [new PaymentCheckoutLine($"Billets — {eventEntity.Title}", total, 1)],
                new Dictionary<string, string> { ["hcbe_ticket_order_id"] = order.Id.ToString("N"), ["hcbe_event_id"] = eventId.ToString("N") },
                order.StripeAccountId, fee > 0 ? fee : null, order.ExpiresAtUtc), ct);
            order.StripeCheckoutSessionId = result.SessionId; order.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct);
            return ApiResponse<TicketCheckoutDto>.SuccessResponse(new(order.Id, order.Status, result.Url, result.SessionId, order.OrderNumber, order.AccessToken, null));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ticket checkout creation failed for {OrderId}", order.Id);
            return await FailCheckout(order, "Unable to start secure checkout", ct);
        }
    }

    public async Task<ApiResponse<TicketOrderDto>> GetOrderByTokenAsync(string token, CancellationToken ct)
    {
        if (!ValidToken(token)) return ApiResponse<TicketOrderDto>.ErrorResponse("Order not found");
        var item = await Orders().AsNoTracking().SingleOrDefaultAsync(value => value.AccessToken == token, ct);
        return item == null ? ApiResponse<TicketOrderDto>.ErrorResponse("Order not found") : ApiResponse<TicketOrderDto>.SuccessResponse(MapOrder(item));
    }

    public async Task<ApiResponse<IReadOnlyList<TicketOrderDto>>> GetMyOrdersAsync(Guid userId, CancellationToken ct)
    {
        var items = await Orders().AsNoTracking().Where(item => item.UserId == userId).OrderByDescending(item => item.CreatedAtUtc).Take(100).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<TicketOrderDto>>.SuccessResponse(items.Select(MapOrder).ToList());
    }

    public async Task<ApiResponse<TicketDto>> TransferTicketAsync(string token, Guid ticketId, TransferTicketRequest request, CancellationToken ct)
    {
        if (!ValidToken(token)) return ApiResponse<TicketDto>.ErrorResponse("Ticket not found");
        var ticket = await context.EventTickets.Include(item => item.Order).ThenInclude(order => order.Event).Include(item => item.Tier)
            .SingleOrDefaultAsync(item => item.Id == ticketId && item.Order.AccessToken == token, ct);
        if (ticket == null || ticket.Status != "Valid" || ticket.Order.Event.Date <= DateTime.UtcNow) return ApiResponse<TicketDto>.ErrorResponse("Ticket cannot be transferred");
        ticket.AttendeeName = request.AttendeeName.Trim(); ticket.AttendeeEmail = request.AttendeeEmail.Trim().ToLowerInvariant(); ticket.TransferredAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<TicketDto>.SuccessResponse(MapTicket(ticket));
    }

    public async Task<ApiResponse<TicketingDashboardDto>> GetDashboardAsync(Guid eventId, CancellationToken ct)
    {
        var orders = await Orders().AsNoTracking().Where(item => item.EventId == eventId).OrderByDescending(item => item.CreatedAtUtc).ToListAsync(ct);
        var paid = orders.Where(item => item.Status is TicketOrderStatuses.Paid or TicketOrderStatuses.PartiallyRefunded or TicketOrderStatuses.Refunded).ToList();
        return ApiResponse<TicketingDashboardDto>.SuccessResponse(new(orders.Count, paid.Sum(item => item.Tickets.Count), paid.Sum(item => item.Tickets.Count(ticket => ticket.CheckedInAtUtc != null)),
            paid.Sum(item => item.TotalCents), paid.Sum(item => item.RefundedAmountCents), orders.FirstOrDefault()?.Currency ?? "cad", orders.Take(50).Select(MapOrder).ToList()));
    }

    public async Task<ApiResponse<TicketDto>> CheckInAsync(Guid eventId, string code, CancellationToken ct)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var ticket = await context.EventTickets.Include(item => item.Order).Include(item => item.Tier)
            .SingleOrDefaultAsync(item => item.Order.EventId == eventId && item.TicketCode == normalized, ct);
        if (ticket == null) return ApiResponse<TicketDto>.ErrorResponse("Ticket not found");
        if (ticket.Status != "Valid") return ApiResponse<TicketDto>.ErrorResponse(ticket.Status == "Used" ? "Ticket already used" : "Ticket is not valid");
        ticket.Status = "Used"; ticket.CheckedInAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct);
        return ApiResponse<TicketDto>.SuccessResponse(MapTicket(ticket));
    }

    public async Task<ApiResponse<TicketOrderDto>> RefundAsync(Guid orderId, RefundTicketOrderRequest request, CancellationToken ct)
    {
        var order = await Orders().SingleOrDefaultAsync(item => item.Id == orderId, ct);
        if (order == null || order.Status is not (TicketOrderStatuses.Paid or TicketOrderStatuses.PartiallyRefunded)) return ApiResponse<TicketOrderDto>.ErrorResponse("Paid order not found");
        if (string.IsNullOrWhiteSpace(order.StripePaymentIntentId)) return ApiResponse<TicketOrderDto>.ErrorResponse("This order cannot be refunded automatically");
        var remaining = order.TotalCents - order.RefundedAmountCents; var amount = request.AmountCents ?? remaining;
        if (amount <= 0 || amount > remaining) return ApiResponse<TicketOrderDto>.ErrorResponse("Invalid refund amount");
        try
        {
            var result = await paymentGateway.RefundAsync(order.StripePaymentIntentId, amount == remaining ? null : amount, request.Reason,
                $"hcbe-ticket-refund-{order.Id:N}-{order.RefundedAmountCents}-{amount}", ct, order.StripeAccountId);
            if (!result.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase)) return ApiResponse<TicketOrderDto>.ErrorResponse("Refund is awaiting payment provider confirmation");
            order.RefundedAmountCents += amount; order.RefundedAtUtc = DateTime.UtcNow; order.UpdatedAtUtc = DateTime.UtcNow;
            order.Status = order.RefundedAmountCents >= order.TotalCents ? TicketOrderStatuses.Refunded : TicketOrderStatuses.PartiallyRefunded;
            if (order.Status == TicketOrderStatuses.Refunded) foreach (var ticket in order.Tickets.Where(item => item.Status == "Valid")) ticket.Status = "Refunded";
            await context.SaveChangesAsync(ct); return ApiResponse<TicketOrderDto>.SuccessResponse(MapOrder(order));
        }
        catch (Exception exception) { logger.LogWarning(exception, "Ticket refund failed for {OrderId}", orderId); return ApiResponse<TicketOrderDto>.ErrorResponse("Payment provider rejected the refund"); }
    }

    public async Task<(byte[]? Content, string FileName)> BuildTicketPdfAsync(string token, CancellationToken ct)
    {
        if (!ValidToken(token)) return (null, "tickets.pdf");
        var order = await Orders().AsNoTracking().SingleOrDefaultAsync(item => item.AccessToken == token && item.PaidAtUtc != null, ct);
        return order == null ? (null, "tickets.pdf") : (ReceiptPdfRenderer.RenderEventTickets(order), $"HCBE-{order.OrderNumber}-billets.pdf");
    }

    private IQueryable<EventTicketOrder> Orders() => context.EventTicketOrders.Include(item => item.Event).Include(item => item.Items).Include(item => item.Tickets).ThenInclude(item => item.Tier);
    private async Task<Dictionary<Guid, (int Sold, int Reserved)>> AvailabilityAsync(Guid eventId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rows = await context.EventTicketOrderItems.AsNoTracking().Where(item => item.Tier.EventId == eventId &&
            (item.Order.Status == TicketOrderStatuses.Paid || item.Order.Status == TicketOrderStatuses.PartiallyRefunded ||
             (item.Order.Status == TicketOrderStatuses.Pending && item.Order.ExpiresAtUtc > now)))
            .Select(item => new { item.TierId, item.Quantity, item.Order.Status }).ToListAsync(ct);
        return rows.GroupBy(item => item.TierId).ToDictionary(group => group.Key, group =>
            (group.Where(item => item.Status != TicketOrderStatuses.Pending).Sum(item => item.Quantity), group.Where(item => item.Status == TicketOrderStatuses.Pending).Sum(item => item.Quantity)));
    }
    private static TicketTierDto MapTier(EventTicketTier item, IReadOnlyDictionary<Guid, (int Sold, int Reserved)> availability)
    { var state = availability.GetValueOrDefault(item.Id); return new(item.Id, item.EventId, item.Name, item.NameEn, item.Description, item.DescriptionEn, item.PriceCents, item.Currency, item.Quantity, state.Sold, state.Reserved, Math.Max(0, item.Quantity - state.Sold - state.Reserved), item.MaxPerOrder, item.SalesStartUtc, item.SalesEndUtc, item.IsActive, item.DisplayOrder); }
    private static PromoCodeDto MapPromo(EventPromoCode item) => new(item.Id, item.EventId, item.Code, item.PercentOff, item.AmountOffCents, item.MaxRedemptions, item.RedemptionCount, item.StartsAtUtc, item.EndsAtUtc, item.IsActive);
    private TicketOrderDto MapOrder(EventTicketOrder item) => new(item.Id, item.EventId, item.Event.Title, item.Event.TitleEn, item.BuyerName, item.BuyerEmail, item.Status, item.Currency,
        item.SubtotalCents, item.DiscountCents, item.PlatformFeeCents, item.TotalCents, item.RefundedAmountCents, item.OrderNumber, null,
        item.PaidAtUtc != null ? TicketPdfUrl(item) : null, item.CreatedAtUtc, item.PaidAtUtc,
        item.Items.Select(value => new TicketOrderItemDto(value.Id, value.TierId, value.TierName, value.TierNameEn, value.Quantity, value.UnitPriceCents, value.LineTotalCents)).ToList(), item.Tickets.Select(MapTicket).ToList());
    private static TicketDto MapTicket(EventTicket item) => new(item.Id, item.TicketCode, item.TierId, item.Tier?.Name ?? string.Empty, item.Tier?.NameEn, item.AttendeeName, item.AttendeeEmail, item.Status, item.IssuedAtUtc, item.CheckedInAtUtc, item.TransferredAtUtc);
    private string TicketPdfUrl(EventTicketOrder order) => $"{PublicApiUrl}/api/event-commerce/orders/{order.AccessToken}/tickets.pdf";
    private void QueueTicketEmail(EventTicketOrder order, Event eventEntity)
    { var email = emailRenderer.EventMessage(order.BuyerName, eventEntity.Title, "Vos billets / Your tickets", $"Commande {order.OrderNumber} confirmée. Téléchargez vos billets PDF avec le lien ci-dessous. / Order confirmed. Download your PDF tickets below: {TicketPdfUrl(order)}", $"{PublicAppUrl}/billets/commande/{order.AccessToken}"); emailOutbox.Enqueue(order.BuyerEmail, email.Subject, email.HtmlBody, nameof(EventTicketOrder), order.Id); }
    private static void CompleteFreeOrder(EventTicketOrder order, Event eventEntity) { order.Status = TicketOrderStatuses.Paid; order.PaidAtUtc = DateTime.UtcNow; order.UpdatedAtUtc = DateTime.UtcNow; foreach (var line in order.Items) for (var i = 0; i < line.Quantity; i++) order.Tickets.Add(new EventTicket { TierId = line.TierId, TicketCode = TicketCode(), AttendeeName = order.BuyerName, AttendeeEmail = order.BuyerEmail }); }
    private async Task<ApiResponse<TicketCheckoutDto>> FailCheckout(EventTicketOrder order, string message, CancellationToken ct) { order.Status = TicketOrderStatuses.Failed; order.FailureReason = message; order.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); return ApiResponse<TicketCheckoutDto>.ErrorResponse(message); }
    private static string? ValidateTier(UpsertTicketTierRequest request) { if (request.SalesEndUtc <= request.SalesStartUtc) return "Sales end must be after sales start"; if (!request.Currency.Equals("cad", StringComparison.OrdinalIgnoreCase)) return "Only CAD ticket sales are supported"; return null; }
    private static void Apply(EventTicketTier item, UpsertTicketTierRequest request) { item.Name = request.Name.Trim(); item.NameEn = Trim(request.NameEn); item.Description = Trim(request.Description); item.DescriptionEn = Trim(request.DescriptionEn); item.PriceCents = request.PriceCents; item.Currency = request.Currency.Trim().ToLowerInvariant(); item.Quantity = request.Quantity; item.MaxPerOrder = request.MaxPerOrder; item.SalesStartUtc = request.SalesStartUtc; item.SalesEndUtc = request.SalesEndUtc; item.IsActive = request.IsActive; item.DisplayOrder = request.DisplayOrder; item.UpdatedAtUtc = DateTime.UtcNow; }
    private static string NormalizeCode(string? value) => new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).Take(32).ToArray());
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidToken(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
    private static string Token() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    private static string TicketCode() => $"TKT-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
    private static string OrderNumber() => $"HCBE-{DateTime.UtcNow:yyyy}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
}
