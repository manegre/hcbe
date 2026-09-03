namespace HcbeApi.Services;

public sealed record RenderedEmail(string Subject, string HtmlBody);

public interface IEmailTemplateRenderer
{
    RenderedEmail MemberOnboarding(string? firstName, string actionUrl);
    RenderedEmail MemberWelcome(string? firstName, string memberSpaceUrl);
    RenderedEmail AdminWelcome(string? firstName, string email, string temporaryPassword, string adminLoginUrl);
    RenderedEmail AdminPromotion(string? firstName, string adminLoginUrl);
    RenderedEmail PasswordReset(string? firstName, string resetUrl, int expiresInMinutes);
    RenderedEmail PasswordChanged(string? firstName, string memberSpaceUrl);
    RenderedEmail MembershipDecision(string? firstName, bool approved, string actionUrl);
    RenderedEmail Newsletter(string subject, string body, string unsubscribeUrl, bool useEnglish);
    RenderedEmail EventRegistrationUpdate(string? firstName, string eventTitle, DateTime eventDate, string status, string confirmationCode, string eventUrl);
    RenderedEmail ServiceCaseUpdate(string? firstName, string ticketNumber, string subject, string status, string? message, string caseUrl);
}
