using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using System.Text;

namespace HcbeApi.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events")
            .WithTags("Events")
            .WithOpenApi();

        group.MapGet("/{id:guid}/calendar.ics", async (Guid id, IEventRegistrationService registrationService) =>
        {
            var calendar = await registrationService.BuildCalendarAsync(id);
            return calendar.Content is null
                ? Results.NotFound()
                : Results.File(calendar.Content, "text/calendar; charset=utf-8", calendar.FileName);
        })
        .WithName("DownloadEventCalendar")
        .Produces(404);

        group.MapGet("/{id:guid}/certificate.pdf", async (Guid id, HttpContext context, IEventRegistrationService registrationService) =>
        {
            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            var certificate = await registrationService.BuildCertificateAsync(userId, id);
            return certificate.Content is null ? Results.NotFound() : Results.File(certificate.Content, "application/pdf", certificate.FileName);
        })
        .WithName("DownloadEventCertificate")
        .RequireAuthorization("Authenticated");

        group.MapGet("/{id:guid}/survey/me", async (Guid id, HttpContext context, IEventRegistrationService registrationService) =>
            context.GetUserId() is Guid userId
                ? (await registrationService.GetMySurveyAsync(userId, id)).HandleServiceResponse()
                : Results.Unauthorized())
        .WithName("GetMyEventSurvey")
        .RequireAuthorization("Authenticated");

        group.MapPut("/{id:guid}/survey/me", async (Guid id, SubmitEventSurveyRequest request, HttpContext context, IEventRegistrationService registrationService) =>
            context.GetUserId() is Guid userId
                ? (await registrationService.SubmitSurveyAsync(userId, id, request)).HandleServiceResponse()
                : Results.Unauthorized())
        .WithName("SubmitEventSurvey")
        .RequireAuthorization("Authenticated");

        group.MapGet("/registrations/me", async (HttpContext context, IEventRegistrationService registrationService) =>
            context.GetUserId() is Guid userId
                ? (await registrationService.GetMineAsync(userId)).HandleServiceResponse()
                : Results.Unauthorized())
        .WithName("GetMyEventRegistrations")
        .RequireAuthorization("Authenticated");

        group.MapGet("/{id:guid}/registration/me", async (Guid id, HttpContext context, IEventRegistrationService registrationService) =>
            context.GetUserId() is Guid userId
                ? (await registrationService.GetMineForEventAsync(userId, id)).HandleServiceResponse()
                : Results.Unauthorized())
        .WithName("GetMyEventRegistration")
        .RequireAuthorization("Authenticated");

        group.MapPost("/{id:guid}/registrations", async (
            Guid id,
            CreateEventRegistrationRequest request,
            HttpContext context,
            IEventRegistrationService registrationService) =>
            context.GetUserId() is Guid userId
                ? (await registrationService.RegisterAsync(userId, id, request)).ToCreatedResult($"/api/events/{id}/registration/me")
                : Results.Unauthorized())
        .WithName("RegisterForEvent")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<EventRegistrationDto>>(201)
        .Produces(400)
        .Produces(401);

        group.MapPost("/{id:guid}/registration/cancel", async (Guid id, HttpContext context, IEventRegistrationService registrationService) =>
            context.GetUserId() is Guid userId
                ? (await registrationService.CancelAsync(userId, id)).HandleServiceResponse()
                : Results.Unauthorized())
        .WithName("CancelEventRegistration")
        .RequireAuthorization("Authenticated");

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
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await eventService.GetAllForAdminAsync()).HandleServiceResponse();
        })
        .WithName("GetEventsForAdmin")
        .RequireAuthorization();

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, IEventService eventService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await eventService.GetByIdForAdminAsync(id)).HandleServiceResponse();
        })
        .WithName("GetEventForAdmin")
        .RequireAuthorization();

        group.MapGet("/admin/{id:guid}/registrations", async (
            Guid id,
            string? status,
            string? search,
            HttpContext context,
            IEventRegistrationService registrationService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await registrationService.GetForAdminAsync(id, status, search)).HandleServiceResponse();
        })
        .WithName("GetEventRegistrationsForAdmin")
        .RequireAuthorization();

        group.MapGet("/admin/{id:guid}/attendance/stats", async (Guid id, HttpContext context, IEventRegistrationService registrationService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await registrationService.GetStatsAsync(id)).HandleServiceResponse();
        })
        .WithName("GetEventAttendanceStats")
        .RequireAuthorization();

        group.MapGet("/admin/{id:guid}/communications", async (Guid id, HttpContext context, IEventRegistrationService registrationService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await registrationService.GetCommunicationsAsync(id)).HandleServiceResponse();
        })
        .WithName("GetEventCommunications")
        .RequireAuthorization();

        group.MapPost("/admin/{id:guid}/communications", async (Guid id, SendEventCommunicationRequest request, HttpContext context, IEventRegistrationService registrationService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            return (await registrationService.SendCommunicationAsync(userId, id, request)).HandleServiceResponse();
        })
        .WithName("SendEventCommunication")
        .RequireAuthorization();

        group.MapPatch("/admin/{id:guid}/registrations/{registrationId:guid}", async (
            Guid id,
            Guid registrationId,
            UpdateEventRegistrationRequest request,
            HttpContext context,
            IEventRegistrationService registrationService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await registrationService.UpdateForAdminAsync(id, registrationId, request)).HandleServiceResponse();
        })
        .WithName("UpdateEventRegistrationForAdmin")
        .RequireAuthorization();

        group.MapPost("/admin/{id:guid}/registrations/check-in/{confirmationCode}", async (
            Guid id, string confirmationCode, HttpContext context, IEventRegistrationService registrationService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await registrationService.CheckInByCodeAsync(id, confirmationCode)).HandleServiceResponse();
        })
        .WithName("CheckInEventRegistrationForAdmin")
        .RequireAuthorization();

        group.MapGet("/admin/{id:guid}/registrations/export", async (
            Guid id,
            HttpContext context,
            IEventRegistrationService registrationService) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            var response = await registrationService.GetForAdminAsync(id, null, null);
            if (!response.Success || response.Data is null) return response.HandleServiceResponse();

            static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
            var csv = new StringBuilder("Name,Email,Status,Confirmation,RegisteredAt,CheckedInAt,AccessibilityNeeds,AdminNotes\r\n");
            foreach (var item in response.Data)
            {
                csv.Append(Csv(item.MemberName)).Append(',')
                    .Append(Csv(item.MemberEmail)).Append(',')
                    .Append(Csv(item.Status)).Append(',')
                    .Append(Csv(item.ConfirmationCode)).Append(',')
                    .Append(Csv(item.RegisteredAt.ToString("O"))).Append(',')
                    .Append(Csv(item.CheckedInAt?.ToString("O"))).Append(',')
                    .Append(Csv(item.AccessibilityNeeds)).Append(',')
                    .Append(Csv(item.AdminNotes)).Append("\r\n");
            }
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"event-{id}-registrations.csv");
        })
        .WithName("ExportEventRegistrations")
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
            if (!context.HasPermission(AdminPermissions.EventsManage))
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
