using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HcbeApi.Infrastructure;

[AllowAnonymous]
public sealed class CmsHub : Hub;
