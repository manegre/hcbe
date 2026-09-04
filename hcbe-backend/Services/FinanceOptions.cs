namespace HcbeApi.Services;

public sealed class FinanceOptions
{
    public const string SectionName = "Finance";
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Stripe";
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool AutomaticTaxEnabled { get; set; }
    public int MembershipGracePeriodDays { get; set; } = 30;
    public int MinimumDonationCents { get; set; } = 500;
    public string Currency { get; set; } = "cad";
}
