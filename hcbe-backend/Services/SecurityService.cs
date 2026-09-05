using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class SecurityService : ISecurityService
{
    private const int ChallengeLifetimeMinutes = 5;
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _auth;
    private readonly IDataProtector _protector;

    public SecurityService(ApplicationDbContext db, IAuthService auth, IDataProtectionProvider provider)
    {
        _db = db;
        _auth = auth;
        _protector = provider.CreateProtector("HCBE.Security.MFA.v1");
    }

    public async Task<SecureLoginResult> CompleteOrChallengeAsync(AuthSession session, string method, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (session.User.MfaEnabledAtUtc is null || string.IsNullOrWhiteSpace(session.User.MfaSecretProtected))
            return new SecureLoginResult(session, null);

        await _auth.RevokeRefreshTokenAsync(session.RefreshToken, ipAddress);
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        _db.MfaChallenges.Add(new MfaChallenge
        {
            UserId = session.User.Id,
            TokenHash = Hash(rawToken),
            AuthenticationMethod = string.IsNullOrWhiteSpace(method) ? "password" : method,
            IpAddress = ipAddress,
            UserAgent = Trim(userAgent, 500),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(ChallengeLifetimeMinutes)
        });
        AddAudit(session.User, "MfaChallengeCreated", nameof(User), session.User.Id.ToString(), ipAddress, new { method });
        await _db.SaveChangesAsync(cancellationToken);
        return new SecureLoginResult(null, rawToken);
    }

    public async Task<AuthSession?> VerifyChallengeAsync(string challengeToken, string code, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(challengeToken) || string.IsNullOrWhiteSpace(code)) return null;
        var hash = Hash(challengeToken);
        var challenge = await _db.MfaChallenges.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
        if (challenge is null || challenge.ConsumedAtUtc is not null || challenge.ExpiresAtUtc <= DateTime.UtcNow || challenge.FailedAttempts >= 5 || !challenge.User.IsActive)
            return null;

        var valid = VerifyUserCode(challenge.User, code, consumeRecoveryCode: true);
        if (!valid)
        {
            challenge.FailedAttempts++;
            AddAudit(challenge.User, "MfaChallengeFailed", nameof(User), challenge.User.Id.ToString(), ipAddress, new { challenge.FailedAttempts });
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        challenge.ConsumedAtUtc = DateTime.UtcNow;
        AddAudit(challenge.User, "MfaChallengeVerified", nameof(User), challenge.User.Id.ToString(), ipAddress, new { challenge.AuthenticationMethod });
        await _db.SaveChangesAsync(cancellationToken);
        return await _auth.CreateSessionForUserAsync(challenge.User, ipAddress, userAgent);
    }

    public async Task<ApiResponse<MfaEnrollmentDto>> BeginEnrollmentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);
        if (user is null || !user.IsActive) return ApiResponse<MfaEnrollmentDto>.ErrorResponse("Account not found");
        var secret = Base32Encode(RandomNumberGenerator.GetBytes(20));
        user.MfaSecretProtected = _protector.Protect(secret);
        user.MfaEnabledAtUtc = null;
        user.MfaRecoveryCodesJson = null;
        var issuer = Uri.EscapeDataString("HCBE Canada");
        var label = Uri.EscapeDataString($"HCBE Canada:{user.Email}");
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
        AddAudit(user, "MfaEnrollmentStarted", nameof(User), user.Id.ToString(), null, null);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<MfaEnrollmentDto>.SuccessResponse(new MfaEnrollmentDto(secret, uri));
    }

    public async Task<ApiResponse<MfaConfirmationDto>> ConfirmEnrollmentAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.MfaSecretProtected)) return ApiResponse<MfaConfirmationDto>.ErrorResponse("MFA enrollment has not been started");
        if (!VerifyTotp(Unprotect(user.MfaSecretProtected), NormalizeCode(code))) return ApiResponse<MfaConfirmationDto>.ErrorResponse("Invalid verification code");

        var recoveryCodes = Enumerable.Range(0, 10).Select(_ => RecoveryCode()).ToList();
        user.MfaRecoveryCodesJson = JsonSerializer.Serialize(recoveryCodes.Select(value => Hash(NormalizeCode(value))));
        user.MfaEnabledAtUtc = DateTime.UtcNow;
        foreach (var token in await _db.RefreshTokens.Where(item => item.UserId == userId && item.RevokedAtUtc == null).ToListAsync(cancellationToken))
            token.RevokedAtUtc = DateTime.UtcNow;
        AddAudit(user, "MfaEnabled", nameof(User), user.Id.ToString(), null, new { recoveryCodes = recoveryCodes.Count });
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<MfaConfirmationDto>.SuccessResponse(new MfaConfirmationDto(Status(user), recoveryCodes));
    }

    public async Task<ApiResponse<MfaStatusDto>> GetMfaStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user is null ? ApiResponse<MfaStatusDto>.ErrorResponse("Account not found") : ApiResponse<MfaStatusDto>.SuccessResponse(Status(user));
    }

    public async Task<ApiResponse<MfaStatusDto>> DisableMfaAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);
        if (user is null || user.MfaEnabledAtUtc is null) return ApiResponse<MfaStatusDto>.ErrorResponse("MFA is not enabled");
        if (!VerifyUserCode(user, code, consumeRecoveryCode: true)) return ApiResponse<MfaStatusDto>.ErrorResponse("Invalid verification code");
        user.MfaSecretProtected = null;
        user.MfaRecoveryCodesJson = null;
        user.MfaEnabledAtUtc = null;
        foreach (var token in await _db.RefreshTokens.Where(item => item.UserId == userId && item.RevokedAtUtc == null).ToListAsync(cancellationToken))
            token.RevokedAtUtc = DateTime.UtcNow;
        AddAudit(user, "MfaDisabled", nameof(User), user.Id.ToString(), null, null);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<MfaStatusDto>.SuccessResponse(Status(user));
    }

    public async Task<ApiResponse<List<AccountSessionDto>>> GetSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken cancellationToken = default)
    {
        var currentHash = string.IsNullOrWhiteSpace(currentRefreshToken) ? null : Hash(currentRefreshToken);
        var now = DateTime.UtcNow;
        var sessions = await _db.RefreshTokens.AsNoTracking()
            .Where(item => item.UserId == userId && item.RevokedAtUtc == null && item.ExpiresAtUtc > now)
            .OrderByDescending(item => item.LastUsedAtUtc ?? item.CreatedAtUtc)
            .Select(item => new AccountSessionDto(item.Id, item.DeviceName ?? "Unknown device", item.CreatedByIp, item.CreatedAtUtc, item.LastUsedAtUtc, item.ExpiresAtUtc, item.TokenHash == currentHash))
            .ToListAsync(cancellationToken);
        return ApiResponse<List<AccountSessionDto>>.SuccessResponse(sessions);
    }

    public async Task<ApiResponse<bool>> RevokeSessionAsync(Guid userId, Guid sessionId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var token = await _db.RefreshTokens.SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
        if (token is null) return ApiResponse<bool>.ErrorResponse("Session not found");
        if (token.RevokedAtUtc is null) { token.RevokedAtUtc = DateTime.UtcNow; token.RevokedByIp = ipAddress; }
        var user = await _db.Users.FindAsync([userId], cancellationToken);
        AddAudit(user, "SessionRevoked", nameof(RefreshToken), sessionId.ToString(), ipAddress, null);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.SuccessResponse(true);
    }

    public async Task<ApiResponse<int>> RevokeOtherSessionsAsync(Guid userId, string? currentRefreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var currentHash = string.IsNullOrWhiteSpace(currentRefreshToken) ? null : Hash(currentRefreshToken);
        var tokens = await _db.RefreshTokens.Where(item => item.UserId == userId && item.RevokedAtUtc == null && item.TokenHash != currentHash).ToListAsync(cancellationToken);
        foreach (var token in tokens) { token.RevokedAtUtc = DateTime.UtcNow; token.RevokedByIp = ipAddress; }
        var user = await _db.Users.FindAsync([userId], cancellationToken);
        AddAudit(user, "OtherSessionsRevoked", nameof(User), userId.ToString(), ipAddress, new { count = tokens.Count });
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<int>.SuccessResponse(tokens.Count);
    }

    public async Task<ApiResponse<SecurityPostureDto>> GetPostureAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var activeAdmins = await _db.Users.CountAsync(item => item.IsAdmin && item.IsActive, cancellationToken);
        var mfaAdmins = await _db.Users.CountAsync(item => item.IsAdmin && item.IsActive && item.MfaEnabledAtUtc != null, cancellationToken);
        var sessions = await _db.RefreshTokens.CountAsync(item => item.RevokedAtUtc == null && item.ExpiresAtUtc > now, cancellationToken);
        var openIncidents = await _db.SecurityIncidents.CountAsync(item => item.ResolvedAtUtc == null, cancellationToken);
        var oldest = await _db.SecurityIncidents.Where(item => item.ResolvedAtUtc == null).MinAsync(item => (DateTime?)item.ReportedAtUtc, cancellationToken);
        var overdue = await _db.Users.CountAsync(user => user.IsAdmin && user.IsActive && !_db.AdminAccessReviews.Any(review => review.ReviewedUserId == user.Id && review.NextReviewAtUtc > now), cancellationToken);
        return ApiResponse<SecurityPostureDto>.SuccessResponse(new(activeAdmins, mfaAdmins, sessions, openIncidents, overdue, oldest));
    }

    private bool VerifyUserCode(User user, string code, bool consumeRecoveryCode)
    {
        var normalized = NormalizeCode(code);
        if (!string.IsNullOrWhiteSpace(user.MfaSecretProtected) && VerifyTotp(Unprotect(user.MfaSecretProtected), normalized)) return true;
        var hashes = RecoveryHashes(user);
        var match = hashes.FirstOrDefault(item => CryptographicOperations.FixedTimeEquals(Convert.FromHexString(item), Convert.FromHexString(Hash(normalized))));
        if (match is null) return false;
        if (consumeRecoveryCode)
        {
            hashes.Remove(match);
            user.MfaRecoveryCodesJson = JsonSerializer.Serialize(hashes);
        }
        return true;
    }

    private string Unprotect(string value) => _protector.Unprotect(value);
    private static MfaStatusDto Status(User user) => new(user.MfaEnabledAtUtc is not null, user.MfaEnabledAtUtc, RecoveryHashes(user).Count);
    private static List<string> RecoveryHashes(User user)
    {
        try { return string.IsNullOrWhiteSpace(user.MfaRecoveryCodesJson) ? [] : JsonSerializer.Deserialize<List<string>>(user.MfaRecoveryCodesJson) ?? []; }
        catch (JsonException) { return []; }
    }
    private static string NormalizeCode(string value) => value.Replace(" ", "").Replace("-", "").Trim().ToUpperInvariant();
    private static string RecoveryCode() => $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string? Trim(string? value, int length) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(length, value.Trim().Length)];

    private static bool VerifyTotp(string secret, string code)
    {
        if (code.Length != 6 || !int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out _)) return false;
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        for (var offset = -1; offset <= 1; offset++)
            if (Totp(secret, counter + offset) == code) return true;
        return false;
    }

    private static string Totp(string secret, long counter)
    {
        var key = Base32Decode(secret);
        Span<byte> bytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--) { bytes[i] = (byte)(counter & 0xff); counter >>= 8; }
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(bytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var value = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (value % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder(); var buffer = 0; var bits = 0;
        foreach (var b in data) { buffer = (buffer << 8) | b; bits += 8; while (bits >= 5) { output.Append(alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; } }
        if (bits > 0) output.Append(alphabet[(buffer << (5 - bits)) & 31]);
        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>(); var buffer = 0; var bits = 0;
        foreach (var c in input.TrimEnd('=').ToUpperInvariant()) { var value = alphabet.IndexOf(c); if (value < 0) continue; buffer = (buffer << 5) | value; bits += 5; if (bits >= 8) { output.Add((byte)((buffer >> (bits - 8)) & 255)); bits -= 8; } }
        return output.ToArray();
    }

    private void AddAudit(User? user, string action, string entityType, string? entityId, string? ip, object? changes) => _db.AuditLogs.Add(new AuditLog
    {
        UserId = user?.Id,
        UserEmail = user?.Email,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        IpAddress = ip,
        ChangesJson = changes is null ? null : JsonSerializer.Serialize(changes)
    });
}
