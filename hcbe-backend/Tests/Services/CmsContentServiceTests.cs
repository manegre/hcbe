using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Moq;

namespace HcbeApi.Tests.Services;

public sealed class CmsContentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly Mock<ICmsContentNotifier> _notifier = new();

    [Fact]
    public async Task Draft_IsHiddenUntilItIsPublished()
    {
        var service = new CmsContentService(_context, _notifier.Object);

        var draft = await service.UpsertAsync(new UpsertCmsContentRequest(
            "public.home.hero.title", "home", "hero", "text", "Hero title",
            "Bienvenue", "Welcome", false), Guid.NewGuid());

        draft.Success.Should().BeTrue();
        draft.Data!.HasUnpublishedChanges.Should().BeTrue();
        (await service.GetPublishedAsync()).Data!.Items.Should().BeEmpty();

        var published = await service.PublishAsync(draft.Data.Id, Guid.NewGuid());
        published.Success.Should().BeTrue();
        published.Data!.Version.Should().Be(1);
        (await service.GetPublishedAsync()).Data!.Items.Should().ContainSingle(item =>
            item.Key == "public.home.hero.title" && item.ValueFr == "Bienvenue" && item.ValueEn == "Welcome");
        _notifier.Verify(value => value.NotifyPublishedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rollback_PublishesAPreviousRevisionAsANewVersion()
    {
        var service = new CmsContentService(_context, _notifier.Object);
        var first = await service.UpsertAsync(new UpsertCmsContentRequest(
            "public.contact.hero.title", "contact", "hero", "text", "Title",
            "Nous joindre", "Contact us", true), null);
        var second = await service.UpsertAsync(new UpsertCmsContentRequest(
            "public.contact.hero.title", "contact", "hero", "text", "Title",
            "Écrivez-nous", "Write to us", true), null);

        var rollback = await service.RollbackAsync(second.Data!.Id, first.Data!.Version, null);

        rollback.Success.Should().BeTrue();
        rollback.Data!.Version.Should().Be(3);
        rollback.Data.PublishedValueFr.Should().Be("Nous joindre");
        (await service.GetRevisionsAsync(rollback.Data.Id)).Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task PublishAll_PromotesEveryPendingDraft()
    {
        var service = new CmsContentService(_context, _notifier.Object);
        await service.UpsertAsync(new UpsertCmsContentRequest(
            "public.home.hero.title", "home", "hero", "text", null, "Accueil", "Home", false), null);
        await service.UpsertAsync(new UpsertCmsContentRequest(
            "seo.home.description", "home", "seo", "seo", null, "Description", "Description", false), null);

        var result = await service.PublishAllAsync(null);

        result.Success.Should().BeTrue();
        result.Data!.PublishedCount.Should().Be(2);
        (await service.GetPublishedAsync()).Data!.Items.Should().HaveCount(2);
        _notifier.Verify(value => value.NotifyPublishedAsync(result.Data.Version, It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose() => _context.Dispose();
}
