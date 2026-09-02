using HcbeApi.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace HcbeApi.Services;

public sealed class CmsContentNotifier(IHubContext<CmsHub> hubContext) : ICmsContentNotifier
{
    public Task NotifyPublishedAsync(long version, CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync("ContentPublished", version, cancellationToken);
}
