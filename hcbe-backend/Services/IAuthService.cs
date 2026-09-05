using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IAuthService
{
    Task<string?> RegisterAsync(string email, string password, string? firstName, string? lastName);
    Task<string?> LoginAsync(string email, string password);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<AuthSession?> CreateSessionAsync(string email, string password, string? ipAddress, string? userAgent = null);
    Task<AuthSession?> CreateExternalSessionAsync(
        string email,
        string? firstName,
        string? lastName,
        bool requireAdmin,
        string? ipAddress,
        string? userAgent = null);
    Task<AuthSession?> CreateOrLinkMemberExternalSessionAsync(
        string email,
        string? firstName,
        string? lastName,
        string? ipAddress,
        string? userAgent = null);
    Task<AuthSession?> RotateRefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent = null);
    Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress);
    Task<AuthSession> CreateSessionForUserAsync(User user, string? ipAddress, string? userAgent = null);
    Task<User?> CompleteRequiredPasswordChangeAsync(Guid userId, string password);
    string CreateToken(User user);
}

