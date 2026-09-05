using System.Globalization;
using System.Security.Cryptography;
using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class SecurityServiceTests : IDisposable
{
    private readonly ApplicationDbContext db = TestDbContextFactory.CreateInMemoryContext();
    private readonly AuthService auth;
    private readonly SecurityService security;

    public SecurityServiceTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Secret"] = "TestSecretKeyThatIsAtLeast32CharactersLong!",
            ["JwtSettings:Issuer"] = "TestIssuer", ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:ExpirationInMinutes"] = "15", ["JwtSettings:RefreshTokenExpirationInDays"] = "7"
        }).Build();
        auth = new AuthService(db, configuration);
        security = new SecurityService(db, auth, new EphemeralDataProtectionProvider());
    }

    [Fact]
    public async Task Enrollment_EnablesTotpAndIssuesSingleUseRecoveryCodes()
    {
        var user = await AddUserAsync();
        var enrollment = await security.BeginEnrollmentAsync(user.Id);
        var confirmation = await security.ConfirmEnrollmentAsync(user.Id, CurrentTotp(enrollment.Data!.Secret));

        confirmation.Success.Should().BeTrue();
        confirmation.Data!.Status.Enabled.Should().BeTrue();
        confirmation.Data.RecoveryCodes.Should().HaveCount(10);
        (await db.Users.FindAsync(user.Id))!.MfaSecretProtected.Should().NotBe(enrollment.Data.Secret);

        var session = await auth.CreateSessionForUserAsync(user, "127.0.0.1", "Mozilla/5.0 Windows Chrome/120");
        var challenge = await security.CompleteOrChallengeAsync(session, "password", "127.0.0.1", "Mozilla/5.0 Windows Chrome/120");
        challenge.RequiresMfa.Should().BeTrue();
        var verified = await security.VerifyChallengeAsync(challenge.ChallengeToken!, confirmation.Data.RecoveryCodes[0], "127.0.0.1", "Mozilla/5.0 Windows Chrome/120");
        verified.Should().NotBeNull();
        (await security.GetMfaStatusAsync(user.Id)).Data!.RecoveryCodesRemaining.Should().Be(9);
        (await security.VerifyChallengeAsync(challenge.ChallengeToken!, confirmation.Data.RecoveryCodes[0], null, null)).Should().BeNull();
    }

    [Fact]
    public async Task Sessions_CanRevokeEveryOtherDeviceWithoutRevokingCurrent()
    {
        var user = await AddUserAsync();
        var current = await auth.CreateSessionForUserAsync(user, "10.0.0.1", "Mozilla/5.0 Windows Chrome/120");
        await auth.CreateSessionForUserAsync(user, "10.0.0.2", "Mozilla/5.0 iPhone Safari/17");

        var before = await security.GetSessionsAsync(user.Id, current.RefreshToken);
        before.Data.Should().HaveCount(2).And.ContainSingle(item => item.IsCurrent && item.DeviceName.Contains("Windows"));
        (await security.RevokeOtherSessionsAsync(user.Id, current.RefreshToken, "10.0.0.1")).Data.Should().Be(1);
        (await security.GetSessionsAsync(user.Id, current.RefreshToken)).Data.Should().ContainSingle(item => item.IsCurrent);
    }

    [Fact]
    public async Task RecoveryCodes_CanBeRegeneratedAndOldCodesBecomeInvalid()
    {
        var user = await AddUserAsync();
        var enrollment = await security.BeginEnrollmentAsync(user.Id);
        var first = await security.ConfirmEnrollmentAsync(user.Id, CurrentTotp(enrollment.Data!.Secret));
        var regenerated = await security.RegenerateRecoveryCodesAsync(user.Id, CurrentTotp(enrollment.Data.Secret));

        regenerated.Success.Should().BeTrue();
        regenerated.Data!.RecoveryCodes.Should().HaveCount(10).And.NotIntersectWith(first.Data!.RecoveryCodes);
        var session = await auth.CreateSessionForUserAsync(user, null, null);
        var challenge = await security.CompleteOrChallengeAsync(session, "password", null, null);
        (await security.VerifyChallengeAsync(challenge.ChallengeToken!, first.Data.RecoveryCodes[0], null, null)).Should().BeNull();
    }

    [Fact]
    public async Task AdministratorSessions_CanBeReviewedAndRevokedBySecurityManager()
    {
        var actor = await AddUserAsync(); actor.IsAdmin = true;
        var affected = await AddUserAsync(); affected.IsAdmin = true; await db.SaveChangesAsync();
        var session = await auth.CreateSessionForUserAsync(affected, "10.0.0.7", "Mozilla/5.0 Android Chrome/120");

        var listed = await security.GetAdminSessionsAsync(actor.Id);
        listed.Data.Should().ContainSingle(item => item.UserId == affected.Id && item.DeviceName.Contains("Android"));
        (await security.RevokeAdminSessionAsync(actor.Id, listed.Data!.Single().Id, "10.0.0.1")).Success.Should().BeTrue();
        (await security.GetAdminSessionsAsync(actor.Id)).Data.Should().BeEmpty();
        db.AuditLogs.Should().Contain(item => item.Action == "AdminSessionRevoked" && item.UserId == actor.Id);
    }

    [Fact]
    public async Task Posture_FlagsAdminsWithoutCurrentAccessReview()
    {
        var user = await AddUserAsync(); user.IsAdmin = true; await db.SaveChangesAsync();
        var posture = await security.GetPostureAsync();
        posture.Data!.ActiveAdmins.Should().Be(1);
        posture.Data.OverdueAccessReviews.Should().Be(1);
    }

    private async Task<User> AddUserAsync()
    {
        var user = new User { Email = $"user-{Guid.NewGuid():N}@hcbe.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword123!"), IsActive = true };
        db.Users.Add(user); await db.SaveChangesAsync(); return user;
    }

    private static string CurrentTotp(string secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>(); var buffer = 0; var bits = 0;
        foreach (var c in secret) { buffer = (buffer << 5) | alphabet.IndexOf(c); bits += 5; if (bits >= 8) { output.Add((byte)((buffer >> (bits - 8)) & 255)); bits -= 8; } }
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30; Span<byte> bytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--) { bytes[i] = (byte)(counter & 0xff); counter >>= 8; }
        using var hmac = new HMACSHA1(output.ToArray()); var hash = hmac.ComputeHash(bytes.ToArray()); var offset = hash[^1] & 0x0f;
        var value = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (value % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    public void Dispose() => db.Dispose();
}
