using HcbeApi.Models;
using HcbeApi.Helpers;

namespace HcbeApi.Services;

public sealed record SecureLoginResult(AuthSession? Session, string? ChallengeToken)
{
    public bool RequiresMfa => ChallengeToken is not null;
}

public interface ISecurityService
{
    Task<SecureLoginResult> CompleteOrChallengeAsync(AuthSession session, string method, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<AuthSession?> VerifyChallengeAsync(string challengeToken, string code, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<ApiResponse<MfaEnrollmentDto>> BeginEnrollmentAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<MfaConfirmationDto>> ConfirmEnrollmentAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<ApiResponse<MfaStatusDto>> GetMfaStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<MfaStatusDto>> DisableMfaAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<ApiResponse<MfaConfirmationDto>> RegenerateRecoveryCodesAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AccountSessionDto>>> GetSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> RevokeSessionAsync(Guid userId, Guid sessionId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> RevokeOtherSessionsAsync(Guid userId, string? currentRefreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AdminAccountSessionDto>>> GetAdminSessionsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> RevokeAdminSessionAsync(Guid currentUserId, Guid sessionId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ApiResponse<SecurityPostureDto>> GetPostureAsync(CancellationToken cancellationToken = default);
}
