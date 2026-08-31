using Google.Apis.Auth;

namespace HcbeApi.Services;

public sealed record GoogleIdentity(string Email, string? FirstName, string? LastName);

public interface IGoogleIdentityTokenValidator
{
    bool IsConfigured { get; }
    Task<GoogleIdentity?> ValidateAsync(string credential, CancellationToken cancellationToken = default);
}

public sealed class GoogleIdentityTokenValidator : IGoogleIdentityTokenValidator
{
    private readonly string? _clientId;
    private readonly bool _enabled;

    public GoogleIdentityTokenValidator(IConfiguration configuration)
    {
        _clientId = configuration["Authentication:Google:ClientId"]?.Trim();
        _enabled = configuration.GetValue("Authentication:Google:Enabled", false);
    }

    public bool IsConfigured => _enabled && !string.IsNullOrWhiteSpace(_clientId);

    public async Task<GoogleIdentity?> ValidateAsync(
        string credential,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(credential))
        {
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_clientId!]
                });

            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            return new GoogleIdentity(
                payload.Email.Trim().ToLowerInvariant(),
                payload.GivenName,
                payload.FamilyName);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
    }
}
