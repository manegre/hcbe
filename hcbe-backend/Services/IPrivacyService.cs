using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IPrivacyService
{
    Task<byte[]?> ExportAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApiResponse<PrivacyRequestDto>> RequestDeletionAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApiResponse> CancelDeletionAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> ProcessDueDeletionsAsync(CancellationToken cancellationToken);
    Task<int> PurgeExpiredOperationalDataAsync(CancellationToken cancellationToken);
}
