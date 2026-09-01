namespace HcbeApi.Services;

public sealed record RenderedEmail(string Subject, string HtmlBody);

public interface IEmailTemplateRenderer
{
    RenderedEmail MemberOnboarding(string? firstName, string actionUrl);
    RenderedEmail MemberWelcome(string? firstName, string memberSpaceUrl);
    RenderedEmail PasswordReset(string? firstName, string resetUrl, int expiresInMinutes);
    RenderedEmail PasswordChanged(string? firstName, string memberSpaceUrl);
    RenderedEmail MembershipDecision(string? firstName, bool approved, string actionUrl);
    RenderedEmail Newsletter(string subject, string body, string unsubscribeUrl, bool useEnglish);
}
