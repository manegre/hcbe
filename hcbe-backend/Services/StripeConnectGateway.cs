using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace HcbeApi.Services;

public sealed class StripeConnectGateway(IHttpClientFactory clients, IOptions<FinanceOptions> finance) : IStripeConnectGateway
{
    private const string Version = "2026-08-26.dahlia";
    private string SecretKey => finance.Value.SecretKey;
    public bool IsEnabled => finance.Value.Enabled && !string.IsNullOrWhiteSpace(SecretKey);

    public async Task<string> CreateAccountAsync(Models.CommunityOrganizer organizer, CancellationToken ct)
    {
        var payload = new
        {
            contact_email = organizer.ContactEmail,
            display_name = organizer.DisplayName,
            dashboard = "full",
            // A community organizer can be an individual, nonprofit, or registered
            // business. Stripe-hosted onboarding collects the legal entity type and
            // verified identity; HCBE intentionally pre-fills only the country.
            identity = new { country = "ca" },
            configuration = new { merchant = new { capabilities = new { card_payments = new { requested = true } } } },
            defaults = new { currency = "cad", responsibilities = new { fees_collector = "stripe", losses_collector = "stripe" }, locales = new[] { "fr-CA", "en-CA" } },
            include = new[] { "configuration.merchant", "identity", "requirements", "defaults" },
            metadata = new Dictionary<string, string> { ["hcbe_organizer_id"] = organizer.Id.ToString("N") }
        };
        using var request = Request(HttpMethod.Post, "v2/core/accounts", payload, $"hcbe-connect-account-{organizer.Id:N}");
        using var response = await clients.CreateClient("StripeConnect").SendAsync(request, ct);
        var root = await ReadAsync(response, ct);
        return root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Stripe did not return an account identifier.");
    }

    public async Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken ct)
    {
        var payload = new { account = accountId, use_case = new { type = "account_onboarding", account_onboarding = new { configurations = new[] { "merchant" }, return_url = returnUrl, refresh_url = refreshUrl, collection_options = new { fields = "eventually_due", future_requirements = "include" } } } };
        using var request = Request(HttpMethod.Post, "v2/core/account_links", payload);
        using var response = await clients.CreateClient("StripeConnect").SendAsync(request, ct);
        var root = await ReadAsync(response, ct);
        return root.GetProperty("url").GetString() ?? throw new InvalidOperationException("Stripe did not return an onboarding link.");
    }

    public async Task<(bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled)> GetStatusAsync(string accountId, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, $"v2/core/accounts/{Uri.EscapeDataString(accountId)}?include%5B%5D=configuration.merchant&include%5B%5D=requirements", null);
        using var response = await clients.CreateClient("StripeConnect").SendAsync(request, ct);
        var root = await ReadAsync(response, ct);
        var due = root.TryGetProperty("requirements", out var requirements) && requirements.ValueKind == JsonValueKind.Object && requirements.TryGetProperty("currently_due", out var currentlyDue)
            ? currentlyDue.GetArrayLength() : 0;
        var merchant = root.GetProperty("configuration").GetProperty("merchant").GetProperty("capabilities");
        var card = CapabilityActive(merchant, "card_payments");
        var payouts = merchant.TryGetProperty("stripe_balance", out var balance) && balance.TryGetProperty("payouts", out var payoutNode) && payoutNode.GetProperty("status").GetString() == "active";
        return (due == 0, card, payouts);
    }

    private HttpRequestMessage Request(HttpMethod method, string path, object? payload, string? idempotencyKey = null)
    {
        if (!IsEnabled) throw new InvalidOperationException("Stripe Connect is not configured.");
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SecretKey);
        request.Headers.TryAddWithoutValidation("Stripe-Version", Version);
        if (idempotencyKey != null) request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        if (payload != null) request.Content = JsonContent.Create(payload);
        return request;
    }

    private static bool CapabilityActive(JsonElement capabilities, string name) => capabilities.TryGetProperty(name, out var node) && node.TryGetProperty("status", out var status) && status.GetString() == "active";
    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var message = "Stripe Connect rejected the request.";
            try { var root = JsonDocument.Parse(body).RootElement; message = root.GetProperty("error").GetProperty("message").GetString() ?? message; } catch (JsonException) { }
            throw new InvalidOperationException(message);
        }
        return JsonDocument.Parse(body).RootElement.Clone();
    }
}
