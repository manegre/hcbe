using FluentAssertions;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HcbeApi.Tests.Services;

public sealed class AppPushServiceTests
{
    [Fact]
    public async Task Subscription_CanBeRegisteredUpdatedAndRemovedPerUser()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new User { Email = "push@hcbe.invalid", FirstName = "Awa", LastName = "Test", PasswordHash = "hash" };
        context.Users.Add(user); await context.SaveChangesAsync();
        var service = new AppPushService(context, Options.Create(new WebPushOptions { PublicKey = "public", PrivateKey = "private" }), NullLogger<AppPushService>.Instance);
        var request = new WebPushSubscriptionRequest("https://push.example.test/subscription/123", "p256dh", "auth");

        (await service.SubscribeAsync(user.Id, request, "Mozilla/5.0 Windows Chrome/120")).Data!.DeviceCount.Should().Be(1);
        (await service.SubscribeAsync(user.Id, request with { Auth = "updated-auth" }, "Mozilla/5.0 Android Chrome/120")).Data!.DeviceCount.Should().Be(1);
        context.WebPushSubscriptions.Single().Auth.Should().Be("updated-auth");

        (await service.UnsubscribeAsync(user.Id, request.Endpoint)).Success.Should().BeTrue();
        (await service.GetStatusAsync(user.Id)).Data!.DeviceCount.Should().Be(0);
    }

    [Fact]
    public void Configuration_DoesNotExposePrivateVapidKey()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var service = new AppPushService(context, Options.Create(new WebPushOptions { PublicKey = "public", PrivateKey = "private" }), NullLogger<AppPushService>.Instance);
        service.GetConfiguration().Should().BeEquivalentTo(new WebPushConfigurationDto(true, "public"));
    }
}
