using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IEventCommerceService
{
    Task<ApiResponse<IReadOnlyList<TicketTierDto>>> GetTiersAsync(Guid eventId, bool admin, CancellationToken ct);
    Task<ApiResponse<TicketTierDto>> CreateTierAsync(Guid eventId, UpsertTicketTierRequest request, CancellationToken ct);
    Task<ApiResponse<TicketTierDto>> UpdateTierAsync(Guid eventId, Guid tierId, UpsertTicketTierRequest request, CancellationToken ct);
    Task<ApiResponse> DeleteTierAsync(Guid eventId, Guid tierId, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<PromoCodeDto>>> GetPromoCodesAsync(Guid eventId, CancellationToken ct);
    Task<ApiResponse<PromoCodeDto>> CreatePromoCodeAsync(Guid eventId, UpsertPromoCodeRequest request, CancellationToken ct);
    Task<ApiResponse> DeletePromoCodeAsync(Guid eventId, Guid promoId, CancellationToken ct);
    Task<ApiResponse<TicketCheckoutDto>> CreateCheckoutAsync(Guid? userId, Guid eventId, CreateTicketCheckoutRequest request, CancellationToken ct);
    Task<ApiResponse<TicketOrderDto>> GetOrderByTokenAsync(string token, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<TicketOrderDto>>> GetMyOrdersAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<TicketDto>> TransferTicketAsync(string token, Guid ticketId, TransferTicketRequest request, CancellationToken ct);
    Task<ApiResponse<TicketingDashboardDto>> GetDashboardAsync(Guid eventId, CancellationToken ct);
    Task<ApiResponse<TicketDto>> CheckInAsync(Guid eventId, string code, CancellationToken ct);
    Task<ApiResponse<TicketOrderDto>> RefundAsync(Guid orderId, RefundTicketOrderRequest request, CancellationToken ct);
    Task<(byte[]? Content, string FileName)> BuildTicketPdfAsync(string token, CancellationToken ct);
}
