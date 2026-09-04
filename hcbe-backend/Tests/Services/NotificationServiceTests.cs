using FluentAssertions;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;

namespace HcbeApi.Tests.Services;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task MemberScope_DoesNotExposeGlobalAdminNotifications()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        context.Notifications.AddRange(
            new Notification { Title = "Admin only", UserId = null },
            new Notification { Title = "Member", UserId = userId },
            new Notification { Title = "Another member", UserId = Guid.NewGuid() });
        await context.SaveChangesAsync();
        var service = new NotificationService(context);

        var result = await service.GetNotificationsAsync(userId, 20);

        result.Data.Should().ContainSingle(item => item.Title == "Member");
        (await service.GetNotificationsAsync(null, 20)).Data.Should().ContainSingle(item => item.Title == "Admin only");
    }
}
