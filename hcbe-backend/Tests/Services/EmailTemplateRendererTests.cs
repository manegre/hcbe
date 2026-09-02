using FluentAssertions;
using HcbeApi.Services;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer = new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:ContactAddress"] = "contact@hcbe.ca"
        })
        .Build());

    [Fact]
    public void MemberOnboarding_RendersResponsiveBrandedEmailWithSafeAction()
    {
        var email = _renderer.MemberOnboarding("Awa <script>", "https://hcbe.ca/espace-membre");

        email.Subject.Should().Contain("Complete your profile");
        email.HtmlBody.Should().Contain("<!doctype html>");
        email.HtmlBody.Should().Contain("@media only screen");
        email.HtmlBody.Should().Contain("HCBE <span");
        email.HtmlBody.Should().Contain("https://hcbe.ca/espace-membre");
        email.HtmlBody.Should().Contain("Awa &lt;script&gt;");
        email.HtmlBody.Should().NotContain("Awa <script>");
    }

    [Fact]
    public void Newsletter_EncodesEditorContentAndIncludesUnsubscribeLink()
    {
        var email = _renderer.Newsletter(
            "Community update",
            "Hello\n<script>alert('x')</script>",
            "https://api.hcbe.ca/api/newsletter/unsubscribe?token=abc",
            useEnglish: true);

        email.HtmlBody.Should().Contain("Hello<br>&lt;script&gt;");
        email.HtmlBody.Should().NotContain("<script>alert");
        email.HtmlBody.Should().Contain("Unsubscribe");
        email.HtmlBody.Should().Contain("token=abc");
    }

    [Fact]
    public void PasswordReset_IncludesExpiryAndSecurityNotice()
    {
        var email = _renderer.PasswordReset("Mariam", "https://hcbe.ca/espace-membre?resetToken=ABC", 30);

        email.Subject.Should().Contain("Password reset");
        email.HtmlBody.Should().Contain("30 minutes");
        email.HtmlBody.Should().Contain("never ask for your password");
        email.HtmlBody.Should().Contain("resetToken=ABC");
    }

    [Fact]
    public void AdminWelcome_IncludesTemporaryAccessAndBothDestinations()
    {
        var email = _renderer.AdminWelcome(
            "Awa",
            "awa@example.com",
            "Temporary!2026Access",
            "https://hcbe.ca/admin/login");

        email.Subject.Should().Contain("Administrator access");
        email.HtmlBody.Should().Contain("awa@example.com");
        email.HtmlBody.Should().Contain("Temporary!2026Access");
        email.HtmlBody.Should().Contain("https://hcbe.ca/admin/login");
        email.HtmlBody.Should().Contain("espace membre");
    }
}
