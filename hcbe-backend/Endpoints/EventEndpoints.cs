using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events")
            .WithTags("Events")
            .WithOpenApi();

        group.MapGet("/", async (IEventService eventService) =>
        {
            var response = await eventService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetEvents")
        .Produces<ApiResponse<List<EventDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await eventService.GetAllForAdminAsync()).HandleServiceResponse();
        })
        .WithName("GetEventsForAdmin")
        .RequireAuthorization();

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await eventService.GetByIdForAdminAsync(id)).HandleServiceResponse();
        })
        .WithName("GetEventForAdmin")
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IEventService eventService) =>
        {
            var response = await eventService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetEvent")
        .Produces<ApiResponse<EventDto>>()
        .Produces(404)
        .Produces(400);

        group.MapPost("/", async (CreateEventRequest request, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await eventService.CreateAsync(request);
            return response.HandleServiceResponse($"/api/events/{response.Data?.Id}");
        })
        .WithName("CreateEvent")
        .RequireAuthorization()
        .Produces<ApiResponse<EventDto>>(201)
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateEventRequest request, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await eventService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateEvent")
        .RequireAuthorization()
        .Produces<ApiResponse<EventDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await eventService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteEvent")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/media/photos", async (Guid id, HttpRequest request, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(ApiResponse<EventMediaDto>.ErrorResponse("Request must be multipart/form-data"));
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<EventMediaDto>.ErrorResponse("No file uploaded"));
            }

            var response = await eventService.AddPhotoAsync(id, file);
            return response.HandleServiceResponse($"/api/events/{id}/media/{response.Data?.Id}");
        })
        .WithName("UploadEventPhoto")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<ApiResponse<EventMediaDto>>(201)
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/media/videos", async (Guid id, AddEventVideoRequest request, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await eventService.AddVideoAsync(id, request);
            return response.HandleServiceResponse($"/api/events/{id}/media/{response.Data?.Id}");
        })
        .WithName("AddEventVideo")
        .RequireAuthorization()
        .Produces<ApiResponse<EventMediaDto>>(201)
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}/media/{mediaId:guid}", async (Guid id, Guid mediaId, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await eventService.DeleteMediaAsync(id, mediaId);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteEventMedia")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/attachments", async (Guid id, HttpRequest request, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(ApiResponse<EventAttachmentDto>.ErrorResponse("Request must be multipart/form-data"));
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<EventAttachmentDto>.ErrorResponse("No file uploaded"));
            }

            var response = await eventService.AddAttachmentAsync(id, file);
            return response.HandleServiceResponse($"/api/events/{id}/attachments/{response.Data?.Id}");
        })
        .WithName("UploadEventAttachment")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<ApiResponse<EventAttachmentDto>>(201)
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId, HttpContext context, IEventService eventService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await eventService.DeleteAttachmentAsync(id, attachmentId);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteEventAttachment")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
