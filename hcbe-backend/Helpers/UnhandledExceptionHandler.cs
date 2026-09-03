using System.Net;
using System.Security.Cryptography;
using System.Text;
using HcbeApi.Data;
using HcbeApi.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Helpers;

public sealed class UnhandledExceptionHandler(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
        logger.LogError(exception,
            "Unhandled request failure. TraceId={TraceId} Method={Method} Path={Path}",
            traceId, httpContext.Request.Method, httpContext.Request.Path.Value);

        await RecordIncidentAsync(httpContext, exception, traceId, cancellationToken);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred",
            detail: "The incident was recorded. Please retry or contact support with the trace identifier.",
            extensions: new Dictionary<string, object?> { ["traceId"] = traceId })
            .ExecuteAsync(httpContext);
        return true;
    }

    private async Task RecordIncidentAsync(
        HttpContext httpContext,
        Exception exception,
        string traceId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var path = Truncate(httpContext.Request.Path.Value ?? "/", 1000);
            var fingerprint = Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes($"{exception.GetType().FullName}|{httpContext.Request.Method}|{path}")));
            var incident = await db.ErrorIncidents
                .FirstOrDefaultAsync(item => item.Fingerprint == fingerprint && item.ResolvedAtUtc == null,
                    cancellationToken);

            var shouldAlert = incident is null || incident.LastAlertedAtUtc is null ||
                incident.LastAlertedAtUtc < now.AddMinutes(-60);
            if (incident is null)
            {
                incident = new ErrorIncident
                {
                    Fingerprint = fingerprint,
                    FirstOccurredAtUtc = now
                };
                db.ErrorIncidents.Add(incident);
            }
            else
            {
                incident.OccurrenceCount++;
            }

            incident.TraceId = Truncate(traceId, 200);
            incident.HttpMethod = Truncate(httpContext.Request.Method, 10);
            incident.Path = path;
            incident.ExceptionType = Truncate(exception.GetType().FullName ?? exception.GetType().Name, 500);
            incident.Message = Truncate(exception.Message, 2000);
            incident.StackTrace = Truncate(exception.ToString(), 8000);
            incident.LastOccurredAtUtc = now;

            if (shouldAlert)
            {
                incident.LastAlertedAtUtc = now;
                db.Notifications.Add(new Notification
                {
                    Type = "system-error",
                    Title = "Erreur de production détectée",
                    Message = $"{incident.HttpMethod} {incident.Path} · trace {incident.TraceId}",
                    RelatedEntityId = incident.Id,
                    Link = "/admin/monitoring"
                });

                var alertEmail = configuration["Operations:AlertEmail"];
                if (!string.IsNullOrWhiteSpace(alertEmail))
                {
                    var encodedPath = WebUtility.HtmlEncode(incident.Path);
                    var encodedTrace = WebUtility.HtmlEncode(incident.TraceId);
                    var encodedType = WebUtility.HtmlEncode(incident.ExceptionType);
                    var encodedMessage = WebUtility.HtmlEncode(incident.Message);
                    db.EmailOutboxMessages.Add(new EmailOutboxMessage
                    {
                        Recipient = alertEmail.Trim(),
                        Subject = $"[HCBE] Production error on {incident.Path}",
                        HtmlBody = $"""
                            <h1>Production error detected</h1>
                            <p><strong>Request:</strong> {incident.HttpMethod} {encodedPath}</p>
                            <p><strong>Type:</strong> {encodedType}</p>
                            <p><strong>Message:</strong> {encodedMessage}</p>
                            <p><strong>Trace:</strong> {encodedTrace}</p>
                            <p>Review and resolve it in the HCBE administration monitoring page.</p>
                            """,
                        RelatedEntityType = nameof(ErrorIncident),
                        RelatedEntityId = incident.Id
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception recordingException)
        {
            logger.LogError(recordingException,
                "Unable to persist production incident for TraceId={TraceId}", traceId);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
