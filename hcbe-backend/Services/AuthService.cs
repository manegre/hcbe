using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HcbeApi.Data;
using HcbeApi.Models;
using BCrypt.Net;
using System.Security.Cryptography;

namespace HcbeApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string?> RegisterAsync(string email, string password, string? firstName, string? lastName)
    {
        email = email.Trim().ToLowerInvariant();
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return null; // User already exists
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Create user
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            IsAdmin = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        return CreateToken(user);
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (user == null)
        {
            return null; // User not found
        }

        var now = DateTime.UtcNow;
        if (user.LockoutEndUtc > now)
        {
            return null;
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEndUtc = now.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }
            await _context.SaveChangesAsync();
            return null; // Invalid password
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = now;
        await _context.SaveChangesAsync();

        // Generate JWT token
        return CreateToken(user);
    }

    public async Task<AuthSession?> CreateSessionAsync(string email, string password, string? ipAddress)
    {
        var accessToken = await LoginAsync(email, password);
        if (accessToken == null) return null;

        var user = await GetUserByEmailAsync(email.Trim().ToLowerInvariant());
        return user == null ? null : await CreateRefreshSessionAsync(user, accessToken, ipAddress);
    }

    public async Task<AuthSession?> RotateRefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;
        var hash = HashToken(refreshToken);
        var existing = await _context.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == hash);
        if (existing == null || !existing.IsActive(DateTime.UtcNow)) return null;

        var newRawToken = CreateSecureToken();
        var newHash = HashToken(newRawToken);
        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReplacedByTokenHash = newHash;

        var expiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays());
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newHash,
            ExpiresAtUtc = expiresAt,
            CreatedByIp = ipAddress
        });
        await _context.SaveChangesAsync();

        return new AuthSession(CreateToken(existing.User), newRawToken, expiresAt, existing.User);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = HashToken(refreshToken);
        var existing = await _context.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == hash);
        if (existing == null || existing.RevokedAtUtc != null) return;
        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;
        await _context.SaveChangesAsync();
    }

    private async Task<AuthSession> CreateRefreshSessionAsync(User user, string accessToken, string? ipAddress)
    {
        var rawToken = CreateSecureToken();
        var expiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays());
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = expiresAt,
            CreatedByIp = ipAddress
        });
        await _context.SaveChangesAsync();
        return new AuthSession(accessToken, rawToken, expiresAt, user);
    }

    private int GetRefreshTokenLifetimeDays() =>
        _configuration.GetValue("JwtSettings:RefreshTokenExpirationInDays", 7);

    private static string CreateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public string CreateToken(User user) => GenerateJwtToken(user);

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = jwtSettings["Issuer"] ?? "HcbeApi";
        var audience = jwtSettings["Audience"] ?? "HcbeApi";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "15");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "Member"),
            new Claim("isAdmin", user.IsAdmin.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

