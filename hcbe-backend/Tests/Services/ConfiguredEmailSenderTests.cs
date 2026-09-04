using System.Net;
using System.Text;
using System.Text.Json;
using HcbeApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HcbeApi.Tests.Services;

public class ConfiguredEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WithBrevoApi_SendsTransactionalEmailOverHttps()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, "{\"messageId\":\"test-id\"}");
        var sender = CreateSender(handler, new Dictionary<string, string?>
        {
            ["Email:Mode"] = "BrevoApi",
            ["Email:Brevo:ApiKey"] = "test-api-key",
            ["Email:FromAddress"] = "noreply@hcbe.ca",
            ["Email:FromName"] = "HCBE Canada",
            ["Email:ReplyToAddress"] = "contact@hcbe.ca"
        });

        await sender.SendAsync("member@example.com", "Bienvenue", "<p>Bonjour <strong>membre</strong></p>");

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.RequestUri?.ToString());
        Assert.Equal("test-api-key", handler.ApiKey);

        using var payload = JsonDocument.Parse(handler.Body!);
        var root = payload.RootElement;
        Assert.Equal("noreply@hcbe.ca", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("HCBE Canada", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("member@example.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("contact@hcbe.ca", root.GetProperty("replyTo").GetProperty("email").GetString());
        Assert.Equal("Bienvenue", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Bonjour <strong>membre</strong></p>", root.GetProperty("htmlContent").GetString());
        Assert.Equal("Bonjour membre", root.GetProperty("textContent").GetString());
    }

    [Fact]
    public async Task SendAsync_WithBrevoApiAndMissingKey_FailsBeforeCallingProvider()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, "{}");
        var sender = CreateSender(handler, new Dictionary<string, string?>
        {
            ["Email:Mode"] = "BrevoApi"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("member@example.com", "Bienvenue", "<p>Bonjour</p>"));

        Assert.Contains("Email:Brevo:ApiKey", exception.Message);
        Assert.Null(handler.Method);
    }

    [Fact]
    public async Task SendAsync_WhenBrevoRejectsRequest_ReturnsUsefulProviderError()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.Unauthorized,
            "{\"code\":\"unauthorized\",\"message\":\"Key not found\"}");
        var sender = CreateSender(handler, new Dictionary<string, string?>
        {
            ["Email:Mode"] = "BrevoApi",
            ["Email:Brevo:ApiKey"] = "invalid-key"
        });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendAsync("member@example.com", "Bienvenue", "<p>Bonjour</p>"));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("unauthorized: Key not found", exception.Message);
        Assert.DoesNotContain("invalid-key", exception.Message);
    }

    private static ConfiguredEmailSender CreateSender(
        HttpMessageHandler handler,
        IDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.brevo.com/v3/")
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(value => value.CreateClient("BrevoTransactional")).Returns(client);

        return new ConfiguredEmailSender(
            configuration,
            Mock.Of<IWebHostEnvironment>(),
            factory.Object);
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? ApiKey { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            ApiKey = request.Headers.TryGetValues("api-key", out var values) ? values.Single() : null;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
