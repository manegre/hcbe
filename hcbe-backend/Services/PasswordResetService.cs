using System.Net;
using System.Security.Cryptography;
using System.Text;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class PasswordResetService : IPasswordResetService
{
    private const string GenericMessage = "If the account exists, password reset instructions have been sent.";
    private readonly ApplicationDbContext _context;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IConfiguration _configuration;

    public PasswordResetService(ApplicationDbContext context, IEmailOutbox emailOutbox, IConfiguration configuration)
    {
        _context = context;
        _emailOutbox = emailOutbox;
        _configuration = configuration;
    }

    public async Task<ApiResponse> RequestAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(item => item.Email.ToLower() == email, cancellationToken);
        if (user is null) return ApiResponse.CreateSuccess(GenericMessage);

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        var publicUrl = (_configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var resetUrl = $"{publicUrl}/espace-membre?resetToken={Uri.EscapeDataString(rawToken)}";
        var html = $"<p>Une demande de réinitialisation de mot de passe a été reçue.</p><p><a href=\"{WebUtility.HtmlEncode(resetUrl)}\">Choisir un nouveau mot de passe</a></p><p>Ce lien expire dans 30 minutes.</p>";
        _emailOutbox.Enqueue(user.Email, "Réinitialisation du mot de passe HCBE Canada", html, nameof(PasswordResetToken));
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse.CreateSuccess(GenericMessage);
    }

    public async Task<ApiResponse> ConfirmAsync(ConfirmPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var hash = Hash(request.Token);
        var reset = await _context.PasswordResetTokens
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
        if (reset?.User is null || reset.UsedAt is not null || reset.ExpiresAt <= DateTime.UtcNow)
            return ApiResponse.CreateError("The password reset link is invalid or has expired");

        reset.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        reset.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse.CreateSuccess("Password updated successfully");
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
