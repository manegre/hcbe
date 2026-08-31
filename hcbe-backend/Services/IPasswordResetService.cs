using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IPasswordResetService
{
    Task<ApiResponse> RequestAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken);
    Task<ApiResponse> ConfirmAsync(ConfirmPasswordResetRequest request, CancellationToken cancellationToken);
}
