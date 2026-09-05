using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshCookieName = "hcbe_refresh";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/login", async (
            LoginRequest request,
            HttpContext context,
            IAuthService authService,
            ISecurityService securityService,
            IWebHostEnvironment environment) =>
        {
            var session = await authService.CreateSessionAsync(
                request.Email,
                request.Password,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString());
            if (session == null)
            {
                return Results.Json(ApiResponse<AuthResponse>.ErrorResponse("Invalid email or password"), statusCode: 401);
            }

            var secured = await securityService.CompleteOrChallengeAsync(session, "password", context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
            if (secured.RequiresMfa) return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse(null, null, true, secured.ChallengeToken)));
            session = secured.Session!;
            SetRefreshCookie(context, environment, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            var user = session.User;
            var authResponse = new AuthResponse(session.AccessToken, MapUser(user));
            return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(authResponse));
        })
        .WithName("Login")
        .AllowAnonymous()
        .RequireRateLimiting("Authentication")
        .Produces<ApiResponse<AuthResponse>>()
        .Produces(401);

        group.MapPost("/google/admin", async (
            GoogleLoginRequest request,
            HttpContext context,
            IGoogleIdentityTokenValidator googleTokenValidator,
            IAuthService authService,
            ISecurityService securityService,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!googleTokenValidator.IsConfigured)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.ErrorResponse("Google sign-in is not configured"),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var identity = await googleTokenValidator.ValidateAsync(request.Credential, cancellationToken);
            if (identity == null)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.ErrorResponse("Google sign-in could not be verified"),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var session = await authService.CreateExternalSessionAsync(
                identity.Email,
                identity.FirstName,
                identity.LastName,
                requireAdmin: true,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString());
            if (session == null)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.ErrorResponse("This Google account is not authorized for administration"),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var secured = await securityService.CompleteOrChallengeAsync(session, "google", context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString(), cancellationToken);
            if (secured.RequiresMfa) return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse(null, null, true, secured.ChallengeToken)));
            session = secured.Session!;
            SetRefreshCookie(context, environment, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            var user = session.User;
            return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse(
                session.AccessToken,
                MapUser(user))));
        })
        .WithName("GoogleAdminLogin")
        .AllowAnonymous()
        .RequireRateLimiting("Authentication")
        .Produces<ApiResponse<AuthResponse>>()
        .Produces(401)
        .Produces(503);

        group.MapPost("/google/member", async (
            GoogleLoginRequest request,
            HttpContext context,
            IGoogleIdentityTokenValidator googleTokenValidator,
            IAuthService authService,
            ISecurityService securityService,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!googleTokenValidator.IsConfigured)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.ErrorResponse("Google sign-in is not configured"),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var identity = await googleTokenValidator.ValidateAsync(request.Credential, cancellationToken);
            if (identity == null)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.ErrorResponse("Google sign-in could not be verified"),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var session = await authService.CreateOrLinkMemberExternalSessionAsync(
                identity.Email,
                identity.FirstName,
                identity.LastName,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString());
            if (session == null)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.ErrorResponse(
                        "This Google account could not be activated as an HCBE member"),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var secured = await securityService.CompleteOrChallengeAsync(session, "google", context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString(), cancellationToken);
            if (secured.RequiresMfa) return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse(null, null, true, secured.ChallengeToken)));
            session = secured.Session!;
            SetRefreshCookie(context, environment, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            var user = session.User;
            return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse(
                session.AccessToken,
                MapUser(user))));
        })
        .WithName("GoogleMemberLogin")
        .AllowAnonymous()
        .RequireRateLimiting("Authentication")
        .Produces<ApiResponse<AuthResponse>>()
        .Produces(401)
        .Produces(403)
        .Produces(503);

        group.MapPost("/refresh", async (
            HttpContext context,
            IAuthService authService,
            IWebHostEnvironment environment) =>
        {
            if (!context.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
            {
                return Results.Unauthorized();
            }

            var session = await authService.RotateRefreshTokenAsync(
                refreshToken,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString());
            if (session == null)
            {
                DeleteRefreshCookie(context, environment);
                return Results.Unauthorized();
            }

            SetRefreshCookie(context, environment, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            var user = session.User;
            return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse(
                session.AccessToken,
                MapUser(user))));
        })
        .WithName("RefreshSession")
        .AllowAnonymous()
        .RequireRateLimiting("Authentication");

        group.MapPost("/logout", async (
            HttpContext context,
            IAuthService authService,
            IWebHostEnvironment environment) =>
        {
            if (context.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
            {
                await authService.RevokeRefreshTokenAsync(
                    refreshToken,
                    context.Connection.RemoteIpAddress?.ToString());
            }
            DeleteRefreshCookie(context, environment);
            return Results.NoContent();
        })
        .WithName("Logout")
        .AllowAnonymous();

        group.MapPost("/mfa/verify", async (
            VerifyMfaRequest request,
            HttpContext context,
            ISecurityService securityService,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var session = await securityService.VerifyChallengeAsync(
                request.ChallengeToken,
                request.Code,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString(),
                cancellationToken);
            if (session is null) return Results.Json(ApiResponse<AuthResponse>.ErrorResponse("Invalid or expired verification code"), statusCode: StatusCodes.Status401Unauthorized);
            SetRefreshCookie(context, environment, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse(session.AccessToken, MapUser(session.User))));
        })
        .WithName("VerifyMfaChallenge")
        .AllowAnonymous()
        .RequireRateLimiting("Authentication");

        group.MapPost("/password-reset/request", async (
            RequestPasswordResetRequest request,
            IPasswordResetService service,
            CancellationToken cancellationToken) =>
            (await service.RequestAsync(request, cancellationToken)).HandleServiceResponse())
            .AllowAnonymous()
            .RequireRateLimiting("Authentication");

        group.MapPost("/password-reset/confirm", async (
            ConfirmPasswordResetRequest request,
            IPasswordResetService service,
            CancellationToken cancellationToken) =>
            (await service.ConfirmAsync(request, cancellationToken)).HandleServiceResponse())
            .AllowAnonymous()
            .RequireRateLimiting("Authentication");

        group.MapPost("/password/change-required", async (
            ChangeRequiredPasswordRequest request,
            HttpContext context,
            IAuthService authService) =>
        {
            var userId = context.GetUserId();
            if (userId is null) return Results.Unauthorized();
            if (!PasswordPolicy.IsStrong(request.Password))
            {
                return Results.BadRequest(ApiResponse<UserDto>.ErrorResponse(PasswordPolicy.ValidationMessage));
            }

            var user = await authService.CompleteRequiredPasswordChangeAsync(userId.Value, request.Password);
            return user is null
                ? Results.BadRequest(ApiResponse<UserDto>.ErrorResponse("No required password change is pending"))
                : Results.Ok(ApiResponse<UserDto>.SuccessResponse(MapUser(user)));
        })
        .WithName("CompleteRequiredPasswordChange")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<UserDto>>()
        .Produces(400)
        .Produces(401);

        group.MapGet("/me", async (HttpContext context, IAuthService authService) =>
        {
            var userId = context.GetUserId();
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return Results.NotFound();
            }

            var userDto = MapUser(user);
            return Results.Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
        })
        .WithName("GetCurrentUser")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<UserDto>>()
        .Produces(401)
        .Produces(404);
    }

    private static void SetRefreshCookie(
        HttpContext context,
        IWebHostEnvironment environment,
        string token,
        DateTime expiresAtUtc) =>
        context.Response.Cookies.Append(RefreshCookieName, token, CookieOptions(environment, expiresAtUtc));

    private static void DeleteRefreshCookie(HttpContext context, IWebHostEnvironment environment) =>
        context.Response.Cookies.Delete(RefreshCookieName, CookieOptions(environment, DateTime.UtcNow.AddDays(-1)));

    private static CookieOptions CookieOptions(IWebHostEnvironment environment, DateTime expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = environment.IsProduction(),
        SameSite = environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
        Path = "/api",
        Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
        IsEssential = true
    };

    private static UserDto MapUser(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.IsAdmin,
            user.MemberId, user.MustChangePassword,
            user.IsAdmin ? user.AdminRole : null,
            user.IsAdmin ? AdminAccess.EffectivePermissions(user.AdminRole, user.AdminPermissions) : [],
            user.MfaEnabledAtUtc is not null);
}
