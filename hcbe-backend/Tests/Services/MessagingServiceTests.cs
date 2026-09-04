using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;

namespace HcbeApi.Tests.Services;

public class MessagingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly MessagingService _service;
    private readonly User _awa;
    private readonly User _idrissa;
    private readonly User _outsider;

    public MessagingServiceTests()
    {
        _awa = UserFor("Awa", "Diallo", "awa@messages.test");
        _idrissa = UserFor("Idrissa", "Ouedraogo", "idrissa@messages.test");
        _outsider = UserFor("Mariam", "Traore", "mariam@messages.test");
        _context.Users.AddRange(_awa, _idrissa, _outsider);
        _context.SaveChanges();
        _service = new MessagingService(_context);
    }

    [Fact]
    public async Task StartConversation_RequiresAcceptedRelationship()
    {
        var result = await _service.StartConversationAsync(_awa.Id, new StartConversationRequest(_idrissa.MemberId!.Value));

        result.Success.Should().BeFalse();
        _context.PrivateConversations.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptedConnection_AllowsPrivateMessagesAndReadReceipt()
    {
        await AddAcceptedConnection();
        var conversation = (await _service.StartConversationAsync(_awa.Id, new StartConversationRequest(_idrissa.MemberId!.Value))).Data!;

        var sent = await _service.SendMessageAsync(_awa.Id, conversation.Id, new SendPrivateMessageRequest("Bonjour, heureux de poursuivre notre échange."));
        sent.Success.Should().BeTrue();
        sent.Data!.ReadAt.Should().BeNull();

        var received = await _service.GetMessagesAsync(_idrissa.Id, conversation.Id);
        received.Data.Should().ContainSingle(item => item.Body.Contains("heureux"));
        await _service.MarkConversationReadAsync(_idrissa.Id, conversation.Id);

        var refreshed = await _service.GetMessagesAsync(_awa.Id, conversation.Id);
        refreshed.Data!.Single().ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Conversation_IsInvisibleToUnrelatedMember()
    {
        await AddAcceptedConnection();
        var conversation = (await _service.StartConversationAsync(_awa.Id, new StartConversationRequest(_idrissa.MemberId!.Value))).Data!;

        var result = await _service.GetMessagesAsync(_outsider.Id, conversation.Id);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AdminResolution_CanSuspendReportedConversation()
    {
        await AddAcceptedConnection();
        var conversation = (await _service.StartConversationAsync(_awa.Id, new StartConversationRequest(_idrissa.MemberId!.Value))).Data!;
        var report = (await _service.ReportConversationAsync(_awa.Id, conversation.Id, new ReportConversationRequest("Repeated messages outside the agreed professional topic."))).Data!;

        var resolved = await _service.ResolveReportAsync(report.Id, new ResolveConversationReportRequest("Resolved", "Conversation paused pending review.", true));
        resolved.Success.Should().BeTrue();
        (await _context.PrivateConversations.FindAsync(conversation.Id))!.Status.Should().Be("Suspended");
        (await _service.SendMessageAsync(_idrissa.Id, conversation.Id, new SendPrivateMessageRequest("Another message"))).Success.Should().BeFalse();
    }

    [Fact]
    public async Task BlockingEitherParticipant_DisablesNewMessages()
    {
        await AddAcceptedConnection();
        var conversation = (await _service.StartConversationAsync(_awa.Id, new StartConversationRequest(_idrissa.MemberId!.Value))).Data!;
        _context.MemberBlocks.Add(new MemberBlock { BlockerMemberId = _idrissa.MemberId.Value, BlockedMemberId = _awa.MemberId!.Value });
        await _context.SaveChangesAsync();

        var result = await _service.SendMessageAsync(_awa.Id, conversation.Id, new SendPrivateMessageRequest("This should be blocked"));

        result.Success.Should().BeFalse();
        (await _service.GetEligibleContactsAsync(_awa.Id)).Data.Should().BeEmpty();
    }

    private async Task AddAcceptedConnection()
    {
        _context.ConnectionRequests.Add(new ConnectionRequest
        {
            RequesterMemberId = _awa.MemberId!.Value,
            RecipientMemberId = _idrissa.MemberId!.Value,
            RequesterMember = _awa.Member,
            RecipientMember = _idrissa.Member,
            Message = "Professional connection request",
            Status = "Accepted",
            RespondedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
    }

    private static User UserFor(string firstName, string lastName, string email)
    {
        var member = new Member { FirstName = firstName, LastName = lastName, Email = email };
        return new User { Email = email, MemberId = member.Id, Member = member };
    }

    public void Dispose() => _context.Dispose();
}
