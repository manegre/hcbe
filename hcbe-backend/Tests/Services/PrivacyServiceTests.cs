using System.Text;
using FluentAssertions;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HcbeApi.Tests.Services;

public sealed class PrivacyServiceTests
{
    [Fact]
    public async Task ExportAsync_ContainsMemberDataButNeverPasswordHash()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new User
        {
            Email = "member@example.com",
            FirstName = "Test",
            PasswordHash = "highly-sensitive-password-hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var bytes = await service.ExportAsync(user.Id, CancellationToken.None);
        var json = Encoding.UTF8.GetString(bytes!);

        json.Should().Contain(user.Email);
        json.Should().NotContain(user.PasswordHash);
        json.Should().NotContain("PasswordHash");
    }

    [Fact]
    public async Task ProcessDueDeletionsAsync_AnonymizesAccountAndRevokesSessions()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var member = new Member { FirstName = "Remove", LastName = "Me", Email = "remove-me@example.com" };
        var user = new User
        {
            Email = "remove-me@example.com",
            FirstName = "Remove",
            LastName = "Me",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CurrentPassword123!"),
            Member = member,
            MemberId = member.Id
        };
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "HASH",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        context.PrivacyRequests.Add(new PrivacyRequest
        {
            UserId = user.Id,
            ExecuteAfterUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        var serviceCase = new ServiceCase { MemberId = member.Id, TicketNumber = "HCBE-PRIVACY-1" };
        context.ServiceCases.Add(serviceCase);
        context.ServiceCaseAttachments.Add(new ServiceCaseAttachment
        {
            ServiceCaseId = serviceCase.Id,
            UploadedByUserId = user.Id,
            FileName = "identity.pdf",
            Url = "/uploads/service-cases/identity.pdf"
        });
        context.PublicSubmissions.Add(new PublicSubmission
        {
            Email = user.Email,
            FirstName = "Remove",
            LastName = "Me",
            Details = "Personal contact request"
        });
        var campaign = new NewsletterCampaign { Subject = "Update", Body = "Body" };
        context.NewsletterCampaigns.Add(campaign);
        context.NewsletterDeliveries.Add(new NewsletterDelivery
        {
            Campaign = campaign,
            CampaignId = campaign.Id,
            Recipient = user.Email,
            TrackingToken = "personally-linked-token"
        });
        context.CommunicationConsentEvents.Add(new CommunicationConsentEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Category = "newsletter",
            Action = "OptIn",
            Source = "member-preferences"
        });
        context.CommunityJourneyStates.Add(new CommunityJourneyState
        {
            UserId = user.Id,
            JourneyType = "Reactivation"
        });
        await context.SaveChangesAsync();
        var files = new Mock<IFileStorageService>();
        files.Setup(item => item.DeleteAsync("/uploads/service-cases/identity.pdf")).ReturnsAsync(true);
        var service = CreateService(context, files.Object);

        var processed = await service.ProcessDueDeletionsAsync(CancellationToken.None);

        processed.Should().Be(1);
        user.IsActive.Should().BeFalse();
        user.IsAdmin.Should().BeFalse();
        user.Email.Should().EndWith("@invalid.local");
        (await context.RefreshTokens.CountAsync()).Should().Be(0);
        var request = await context.PrivacyRequests.SingleAsync();
        request.Status.Should().Be("Completed");
        request.UserId.Should().BeNull();
        request.SubjectReference.Should().NotBeNullOrWhiteSpace();
        (await context.ServiceCaseAttachments.CountAsync()).Should().Be(0);
        files.Verify(item => item.DeleteAsync("/uploads/service-cases/identity.pdf"), Times.Once);
        var submission = await context.PublicSubmissions.SingleAsync();
        submission.Email.Should().EndWith("@invalid.local");
        submission.Details.Should().Be("[redacted]");
        var delivery = await context.NewsletterDeliveries.SingleAsync();
        delivery.Recipient.Should().EndWith("@invalid.local");
        delivery.TrackingToken.Should().NotBe("personally-linked-token");
        var consent = await context.CommunicationConsentEvents.SingleAsync();
        consent.UserId.Should().BeNull();
        consent.Email.Should().EndWith("@invalid.local");
        (await context.CommunityJourneyStates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RequestDeletionAsync_ForAdministrator_IsRejected()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new User { Email = "admin@example.com", PasswordHash = "hash", IsAdmin = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await CreateService(context).RequestDeletionAsync(user.Id, CancellationToken.None);

        result.Success.Should().BeFalse();
        (await context.PrivacyRequests.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ProcessDueDeletionsAsync_WhenStorageFails_DoesNotPersistPartialAnonymization()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var member = new Member { FirstName = "Keep", LastName = "Until Retry", Email = "retry@example.com" };
        var user = new User { Email = member.Email, PasswordHash = "hash", Member = member, MemberId = member.Id, IsActive = true };
        var serviceCase = new ServiceCase { MemberId = member.Id, TicketNumber = "HCBE-PRIVACY-RETRY" };
        var privacyRequest = new PrivacyRequest { UserId = user.Id, ExecuteAfterUtc = DateTime.UtcNow.AddMinutes(-1) };
        context.AddRange(user, serviceCase, privacyRequest, new ServiceCaseAttachment
        {
            ServiceCaseId = serviceCase.Id,
            UploadedByUserId = user.Id,
            FileName = "retry.pdf",
            Url = "/uploads/service-cases/retry.pdf"
        });
        await context.SaveChangesAsync();
        var files = new Mock<IFileStorageService>();
        files.Setup(item => item.DeleteAsync("/uploads/service-cases/retry.pdf"))
            .ThrowsAsync(new IOException("Object storage unavailable"));

        await CreateService(context, files.Object).ProcessDueDeletionsAsync(CancellationToken.None);

        var unchangedUser = await context.Users.AsNoTracking().SingleAsync();
        unchangedUser.Email.Should().Be("retry@example.com");
        unchangedUser.IsActive.Should().BeTrue();
        (await context.ServiceCaseAttachments.CountAsync()).Should().Be(1);
        var failedRequest = await context.PrivacyRequests.AsNoTracking().SingleAsync();
        failedRequest.Status.Should().Be("Failed");
        failedRequest.FailureReason.Should().Contain("Object storage unavailable");
    }

    [Fact]
    public async Task RequestDeletionAsync_ImmediatelyWithdrawsOptionalSharingAndCommunications()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var member = new Member { FirstName = "Awa", LastName = "Test", Email = "awa@example.com" };
        var user = new User { Email = member.Email, PasswordHash = "hash", Member = member, MemberId = member.Id };
        var preferences = new MemberPreference
        {
            UserId = user.Id,
            EmailEvents = true,
            EmailOpportunities = true,
            EmailMentorship = true,
            EmailServiceUpdates = true,
            EmailNewsletter = true,
            PushNotifications = true,
            HasCompletedPreferences = true
        };
        var profile = new NetworkingProfile { MemberId = member.Id, IsVisible = true, AllowContactRequests = true };
        var mentorship = new MentorshipApplication { MemberId = member.Id, ConsentToShare = true };
        var newsletter = new NewsletterSubscription
        {
            Email = user.Email,
            FullName = "Awa Test",
            ConsentAcceptedAt = DateTime.UtcNow,
            IsActive = true,
            UnsubscribeToken = "token"
        };
        context.AddRange(user, preferences, profile, mentorship, newsletter);
        await context.SaveChangesAsync();

        var result = await CreateService(context).RequestDeletionAsync(user.Id, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Pending");
        preferences.EmailEvents.Should().BeFalse();
        preferences.EmailOpportunities.Should().BeFalse();
        preferences.EmailMentorship.Should().BeFalse();
        preferences.EmailServiceUpdates.Should().BeFalse();
        preferences.EmailNewsletter.Should().BeFalse();
        preferences.PushNotifications.Should().BeFalse();
        profile.IsVisible.Should().BeFalse();
        profile.AllowContactRequests.Should().BeFalse();
        mentorship.ConsentToShare.Should().BeFalse();
        newsletter.IsActive.Should().BeFalse();
        context.CommunicationConsentEvents.Should().HaveCount(7);
        context.CommunicationConsentEvents.Should().OnlyContain(item =>
            item.Action == "OptOut" && item.Source == "account-deletion-request");
    }

    [Fact]
    public async Task ProcessDueDeletionsAsync_CancelsRecurringBillingBeforeRemovingProviderIdentifiers()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new User { Email = "subscriber@example.com", PasswordHash = "hash", IsActive = true };
        var standing = new MembershipStanding { UserId = user.Id, User = user, Status = MembershipStatuses.Active, AutoRenew = true, StripeCustomerId = "cus_delete", StripeSubscriptionId = "sub_delete", CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(6) };
        var transaction = new FinancialTransaction { UserId = user.Id, Kind = FinanceKinds.Membership, Status = FinanceStatuses.Paid, AmountCents = 5000, PayerEmail = user.Email, PayerName = "Subscriber", ReceiptNumber = "HCBE-TEST-DELETE", ReceiptToken = "receipt-delete", PaidAtUtc = DateTime.UtcNow };
        context.AddRange(user, standing, transaction, new PrivacyRequest { UserId = user.Id, ExecuteAfterUtc = DateTime.UtcNow.AddMinutes(-1) });
        await context.SaveChangesAsync();
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(item => item.IsEnabled).Returns(true);
        gateway.Setup(item => item.CancelSubscriptionAsync("sub_delete", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var processed = await CreateService(context, paymentGateway: gateway.Object).ProcessDueDeletionsAsync(CancellationToken.None);

        processed.Should().Be(1);
        gateway.Verify(item => item.CancelSubscriptionAsync("sub_delete", It.IsAny<CancellationToken>()), Times.Once);
        standing.Status.Should().Be(MembershipStatuses.Inactive);
        standing.AutoRenew.Should().BeFalse();
        standing.StripeCustomerId.Should().BeNull();
        standing.StripeSubscriptionId.Should().BeNull();
        transaction.UserId.Should().BeNull();
        transaction.IsAnonymous.Should().BeTrue();
        transaction.PayerEmail.Should().EndWith("@invalid.local");
    }

    private static PrivacyService CreateService(HcbeApi.Data.ApplicationDbContext context, IFileStorageService? fileStorage = null, IPaymentGateway? paymentGateway = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Privacy:DeletionDelayDays"] = "30",
            ["Privacy:AuditRetentionDays"] = "730"
        }).Build();
        return new PrivacyService(context, configuration, fileStorage ?? Mock.Of<IFileStorageService>(), paymentGateway ?? Mock.Of<IPaymentGateway>(), NullLogger<PrivacyService>.Instance);
    }
}
