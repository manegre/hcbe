using System.Security.Claims;
using HcbeApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Infrastructure;

[Authorize(Policy = "Authenticated")]
public sealed class MessagingHub(ApplicationDbContext context) : Hub
{
    public async Task JoinConversation(Guid conversationId)
    {
        var memberId = await GetMemberIdAsync();
        if (memberId == null) throw new HubException("Member profile required.");
        var authorized = await context.PrivateConversations.AsNoTracking().AnyAsync(conversation =>
            conversation.Id == conversationId &&
            (conversation.MemberOneId == memberId || conversation.MemberTwoId == memberId));
        if (!authorized) throw new HubException("Conversation not found.");
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

    public override async Task OnConnectedAsync()
    {
        var memberId = await GetMemberIdAsync();
        if (memberId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, MemberGroup(memberId.Value));
        await base.OnConnectedAsync();
    }

    private async Task<Guid?> GetMemberIdAsync()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? await context.Users.AsNoTracking().Where(user => user.Id == userId).Select(user => user.MemberId).SingleOrDefaultAsync()
            : null;
    }

    public static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId:N}";
    public static string MemberGroup(Guid memberId) => $"member:{memberId:N}";
}
