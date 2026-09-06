using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class CommunityProgramsEndpoints
{
    public static void MapCommunityProgramsEndpoints(this WebApplication app)
    {
        var publicGroup = app.MapGroup("/api/community-programs").WithTags("Community programs").WithOpenApi();
        publicGroup.MapGet("/directory", async (string? search, string? category, string? province, ICommunityProgramsService service, CancellationToken ct) => (await service.GetDirectoryAsync(search, category, province, ct)).HandleServiceResponse()).AllowAnonymous();
        publicGroup.MapGet("/sponsorship-packages", async (ICommunityProgramsService service, CancellationToken ct) => (await service.GetSponsorshipPackagesAsync(ct)).HandleServiceResponse()).AllowAnonymous();
        publicGroup.MapGet("/annual-reports", async (ICommunityProgramsService service, CancellationToken ct) => (await service.GetPublishedReportsAsync(ct)).HandleServiceResponse()).AllowAnonymous();

        var member = app.MapGroup("/api/community-programs/member").WithTags("Member community programs").RequireAuthorization("Authenticated").WithOpenApi();
        member.MapGet("/businesses", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.GetMyBusinessesAsync(id, ct)));
        member.MapPost("/businesses", (UpsertCommunityBusinessRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.SaveBusinessAsync(null, id, request, ct))).RequireRateLimiting("PublicWrite");
        member.MapPut("/businesses/{id:guid}", (Guid id, UpsertCommunityBusinessRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, userId => service.SaveBusinessAsync(id, userId, request, ct))).RequireRateLimiting("PublicWrite");
        member.MapGet("/newcomer", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.GetJourneyAsync(id, ct)));
        member.MapPut("/newcomer", (UpsertNewcomerJourneyRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.SaveJourneyAsync(id, request, ct)));
        member.MapGet("/family", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.GetHouseholdAsync(id, ct)));
        member.MapPut("/family", (UpsertFamilyHouseholdRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.SaveHouseholdAsync(id, request, ct)));
        member.MapPost("/family/members", (AddFamilyMemberRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.AddFamilyMemberAsync(id, request, ct)));
        member.MapDelete("/family/members/{id:guid}", (Guid id, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, userId => service.RemoveFamilyMemberAsync(userId, id, ct)));
        member.MapGet("/appointments/slots", async (ICommunityProgramsService service, CancellationToken ct) => (await service.GetAvailableSlotsAsync(ct)).HandleServiceResponse());
        member.MapGet("/appointments", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.GetMyBookingsAsync(id, ct)));
        member.MapPost("/appointments", (CreateAppointmentBookingRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.BookAsync(id, request, ct))).RequireRateLimiting("PublicWrite");
        member.MapPost("/appointments/{id:guid}/cancel", (Guid id, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, userId => service.CancelBookingAsync(userId, id, ct)));
        member.MapGet("/benefits", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.GetBenefitsAsync(id, ct)));
        member.MapPost("/benefits/{id:guid}/claim", (Guid id, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, userId => service.ClaimBenefitAsync(userId, id, ct))).RequireRateLimiting("PublicWrite");
        member.MapGet("/grant-applications", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.GetMyGrantApplicationsAsync(id, ct)));
        member.MapPost("/grant-applications", (CreateGrantApplicationRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.ApplyForGrantAsync(id, request, ct))).RequireRateLimiting("PublicWrite");
        member.MapPost("/grant-applications/{id:guid}/withdraw", (Guid id, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, userId => service.WithdrawGrantApplicationAsync(userId, id, ct)));
        member.MapGet("/sponsorships", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.GetMySponsorshipRequestsAsync(id, ct)));
        member.MapPost("/sponsorships", (CreateSponsorshipRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => User(http, id => service.RequestSponsorshipAsync(id, request, ct))).RequireRateLimiting("PublicWrite");

        var admin = app.MapGroup("/api/admin/community-programs").WithTags("Community programs administration").RequireAuthorization().WithOpenApi();
        admin.MapGet("/overview", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.GetAdminOverviewAsync(ct)));
        admin.MapPatch("/businesses/{id:guid}", (Guid id, ReviewCommunityBusinessRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.ReviewBusinessAsync(id, request, ct)));
        admin.MapPost("/appointment-offerings", (UpsertAppointmentOfferingRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.SaveOfferingAsync(null, request, ct)));
        admin.MapPut("/appointment-offerings/{id:guid}", (Guid id, UpsertAppointmentOfferingRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.SaveOfferingAsync(id, request, ct)));
        admin.MapPost("/appointment-slots", (CreateAppointmentSlotRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.CreateSlotAsync(request, ct)));
        admin.MapPost("/benefits", (UpsertPartnerBenefitRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.SaveBenefitAsync(null, request, ct)));
        admin.MapPut("/benefits/{id:guid}", (Guid id, UpsertPartnerBenefitRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.SaveBenefitAsync(id, request, ct)));
        admin.MapPatch("/grant-applications/{id:guid}", (Guid id, ReviewGrantApplicationRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.CommunityManage, () => service.ReviewGrantApplicationAsync(id, request, ct)));
        admin.MapPost("/sponsorship-packages", (UpsertSponsorshipPackageRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.FinanceManage, () => service.SaveSponsorshipPackageAsync(null, request, ct)));
        admin.MapPut("/sponsorship-packages/{id:guid}", (Guid id, UpsertSponsorshipPackageRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.FinanceManage, () => service.SaveSponsorshipPackageAsync(id, request, ct)));
        admin.MapPatch("/sponsorships/{id:guid}", (Guid id, ReviewSponsorshipRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.FinanceManage, () => service.ReviewSponsorshipAsync(id, request, ct)));
        admin.MapPost("/annual-reports/{year:int}/generate", (int year, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.AnalyticsView, () => service.GenerateAnnualReportAsync(year, ct)));
        admin.MapPost("/annual-reports/{id:guid}/publish", (Guid id, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.AnalyticsView, () => service.PublishAnnualReportAsync(id, ct)));
        admin.MapPut("/automations/{id:guid}", (Guid id, UpdateAutomationRuleRequest request, HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.SettingsManage, () => service.UpdateAutomationRuleAsync(id, request, ct)));
        admin.MapPost("/automations/run", (HttpContext http, ICommunityProgramsService service, CancellationToken ct) => Admin(http, AdminPermissions.SettingsManage, () => service.RunDueAutomationsAsync(true, ct)));
    }

    private static async Task<IResult> User<T>(HttpContext http, Func<Guid, Task<ApiResponse<T>>> action) =>
        http.GetUserId() is Guid userId ? (await action(userId)).HandleServiceResponse() : Results.Unauthorized();
    private static async Task<IResult> Admin<T>(HttpContext http, string permission, Func<Task<ApiResponse<T>>> action) =>
        http.HasPermission(permission) ? (await action()).HandleServiceResponse() : Results.Forbid();
}
