using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace HcbeApi.Endpoints;

public static class MessagingEndpoints
{
    public static void MapMessagingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/community/messages").WithTags("Private messaging").RequireAuthorization("Authenticated").WithOpenApi();
        group.MapGet("/contacts", async (HttpContext context, IMessagingService service) =>
            context.GetUserId() is Guid userId ? (await service.GetEligibleContactsAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapGet("/conversations", async (HttpContext context, IMessagingService service) =>
            context.GetUserId() is Guid userId ? (await service.GetConversationsAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/conversations", async (StartConversationRequest request, HttpContext context, IMessagingService service) =>
            context.GetUserId() is Guid userId ? (await service.StartConversationAsync(userId, request)).ToCreatedResult("/api/community/messages/conversations") : Results.Unauthorized());
        group.MapGet("/conversations/{id:guid}", async (Guid id, HttpContext context, IMessagingService service) =>
            context.GetUserId() is Guid userId ? (await service.GetMessagesAsync(userId, id)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/conversations/{id:guid}", async (
            Guid id,
            SendPrivateMessageRequest request,
            HttpContext context,
            IMessagingService service,
            IHubContext<MessagingHub> hub) =>
        {
            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            var response = await service.SendMessageAsync(userId, id, request);
            if (response.Success && response.Data != null)
            {
                await hub.Clients.Group(MessagingHub.ConversationGroup(id))
                    .SendAsync("MessageReceived", response.Data);
            }
            return response.ToCreatedResult($"/api/community/messages/conversations/{id}");
        });
        group.MapPost("/conversations/{id:guid}/read", async (Guid id, HttpContext context, IMessagingService service) =>
            context.GetUserId() is Guid userId ? (await service.MarkConversationReadAsync(userId, id)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/conversations/{id:guid}/report", async (Guid id, ReportConversationRequest request, HttpContext context, IMessagingService service) =>
            context.GetUserId() is Guid userId ? (await service.ReportConversationAsync(userId, id, request)).ToCreatedResult("/api/admin/message-reports") : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/message-reports").WithTags("Message moderation").RequireAuthorization().WithOpenApi();
        admin.MapGet("/", async (string? status, HttpContext context, IMessagingService service) =>
            !context.IsAdmin() ? Results.Forbid() : (await service.GetReportsForAdminAsync(status)).HandleServiceResponse());
        admin.MapPatch("/{id:guid}", async (Guid id, ResolveConversationReportRequest request, HttpContext context, IMessagingService service) =>
            !context.IsAdmin() ? Results.Forbid() : (await service.ResolveReportAsync(id, request)).HandleServiceResponse());
    }
}
