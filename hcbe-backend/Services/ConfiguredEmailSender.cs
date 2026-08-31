using System.Net;
using System.Net.Mail;

namespace HcbeApi.Services;

public class ConfiguredEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ConfiguredEmailSender(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var mode = _configuration["Email:Mode"] ?? "Disabled";
        if (mode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email delivery is disabled. Configure Email:Mode as Smtp or Pickup.");

        using var message = new MailMessage
        {
            From = new MailAddress(
                _configuration["Email:FromAddress"] ?? "noreply@hcbecanada.org",
                _configuration["Email:FromName"] ?? "HCBE Canada"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

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
}
