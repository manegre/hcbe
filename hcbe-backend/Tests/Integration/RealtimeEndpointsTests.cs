using System.Net;
using FluentAssertions;

namespace HcbeApi.Tests.Integration;

public sealed class RealtimeEndpointsTests(CustomWebApplicationFactory factory) :
    IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task MessagingHubNegotiate_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/hubs/messaging/negotiate?negotiateVersion=1",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose() => _client.Dispose();
}
