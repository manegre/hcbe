using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IAuthService
{
    Task<string?> RegisterAsync(string email, string password, string? firstName, string? lastName);
    Task<string?> LoginAsync(string email, string password);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<AuthSession?> CreateSessionAsync(string email, string password, string? ipAddress);
    Task<AuthSession?> CreateExternalSessionAsync(
        string email,
        string? firstName,
        string? lastName,
        bool requireAdmin,
        string? ipAddress);
    Task<AuthSession?> CreateOrLinkMemberExternalSessionAsync(
        string email,
        string? firstName,
        string? lastName,
        string? ipAddress);
    Task<AuthSession?> RotateRefreshTokenAsync(string refreshToken, string? ipAddress);
    Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress);
    string CreateToken(User user);
}

