using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HcbeApi.Services;

public class ConfiguredEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;

    public ConfiguredEmailSender(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var mode = _configuration["Email:Mode"] ?? "Disabled";
        if (mode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email delivery is disabled. Configure Email:Mode as BrevoApi, Smtp, or Pickup.");

        if (mode.Equals("BrevoApi", StringComparison.OrdinalIgnoreCase))
        {
            await SendWithBrevoApiAsync(recipient, subject, htmlBody, cancellationToken);
            return;
        }

        if (!mode.Equals("Smtp", StringComparison.OrdinalIgnoreCase)
            && !mode.Equals("Pickup", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported email delivery mode '{mode}'.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(
                _configuration["Email:FromAddress"] ?? "noreply@hcbe.ca",
                _configuration["Email:FromName"] ?? "HCBE Canada"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(recipient);
        var replyTo = _configuration["Email:ReplyToAddress"] ?? _configuration["Email:ContactAddress"];
        if (!string.IsNullOrWhiteSpace(replyTo)) message.ReplyToList.Add(new MailAddress(replyTo));

        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            ToPlainText(htmlBody),
            Encoding.UTF8,
            MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            htmlBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Html));

        using var client = new SmtpClient();
        if (mode.Equals("Pickup", StringComparison.OrdinalIgnoreCase))
        {
            var configuredPath = _configuration["Email:PickupDirectory"] ?? "App_Data/mail-outbox";
            var pickupPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(_environment.ContentRootPath, configuredPath);
            Directory.CreateDirectory(pickupPath);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = pickupPath;
        }
        else
        {
            client.Host = _configuration["Email:Smtp:Host"]
                ?? throw new InvalidOperationException("Email:Smtp:Host is required");
            client.Port = int.TryParse(_configuration["Email:Smtp:Port"], out var port) ? port : 587;
            client.EnableSsl = !bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var ssl) || ssl;
            var username = _configuration["Email:Smtp:Username"];
            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, _configuration["Email:Smtp:Password"]);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    private async Task SendWithBrevoApiAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Email:Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Email:Brevo:ApiKey is required when Email:Mode is BrevoApi.");

        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@hcbe.ca";
        var fromName = _configuration["Email:FromName"] ?? "HCBE Canada";
        var replyToAddress = _configuration["Email:ReplyToAddress"] ?? _configuration["Email:ContactAddress"];

        _ = new MailAddress(fromAddress);
        _ = new MailAddress(recipient);
        if (!string.IsNullOrWhiteSpace(replyToAddress)) _ = new MailAddress(replyToAddress);

        var payload = new
        {
            sender = new { name = fromName, email = fromAddress },
            to = new[] { new { email = recipient } },
            replyTo = string.IsNullOrWhiteSpace(replyToAddress)
                ? null
                : new { name = fromName, email = replyToAddress },
            subject,
            htmlContent = htmlBody,
            textContent = ToPlainText(htmlBody)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient("BrevoTransactional");
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return;

        var details = await ReadBrevoErrorAsync(response, cancellationToken);
        throw new HttpRequestException(
            $"Brevo API rejected email delivery with HTTP {(int)response.StatusCode}{details}.",
            null,
            response.StatusCode);
    }

    private static async Task<string> ReadBrevoErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var code = root.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null;
            var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;
            var summary = string.Join(": ", new[] { code, message }.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (string.IsNullOrWhiteSpace(summary)) return string.Empty;

            summary = Regex.Replace(summary, "[\\r\\n\\t]+", " ").Trim();
            return $" ({summary[..Math.Min(summary.Length, 300)]})";
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static string ToPlainText(string html)
    {
        var withBreaks = Regex.Replace(html, "<(br|/p|/tr|/h[1-6])[^>]*>", "\n", RegexOptions.IgnoreCase);
        var withoutStyles = Regex.Replace(withBreaks, "<(style|head)[^>]*>.*?</\\1>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var plain = Regex.Replace(withoutStyles, "<[^>]+>", string.Empty);
        plain = WebUtility.HtmlDecode(plain).Replace("‌", string.Empty);
        return Regex.Replace(plain, "[ \t]+", " ").Replace("\r", string.Empty).Trim();
    }
}
