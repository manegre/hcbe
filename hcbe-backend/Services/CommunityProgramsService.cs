using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class CommunityProgramsService(ApplicationDbContext context, IEmailOutbox emailOutbox) : ICommunityProgramsService
{
    private static readonly HashSet<string> ReviewStatuses = new(StringComparer.OrdinalIgnoreCase)
        { CommunityProgramStatuses.Submitted, CommunityProgramStatuses.Approved, CommunityProgramStatuses.Rejected };
    private static readonly string[] JourneySteps = ["orientation", "documents", "health", "housing", "employment", "community"];

    public async Task<ApiResponse<IReadOnlyList<CommunityBusinessDto>>> GetDirectoryAsync(string? search, string? category, string? province, CancellationToken ct)
    {
        var query = context.CommunityBusinesses.AsNoTracking().Where(item => item.Status == CommunityProgramStatuses.Approved);
        if (!string.IsNullOrWhiteSpace(search)) { var value = search.Trim().ToLower(); query = query.Where(item => item.Name.ToLower().Contains(value) || item.Description.ToLower().Contains(value) || (item.Services != null && item.Services.ToLower().Contains(value))); }
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(item => item.Category == category.Trim());
        if (!string.IsNullOrWhiteSpace(province)) query = query.Where(item => item.Province == province.Trim());
        var items = await query.OrderByDescending(item => item.IsFeatured).ThenBy(item => item.Name).Take(250).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<CommunityBusinessDto>>.SuccessResponse(items.Select(MapBusiness).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<CommunityBusinessDto>>> GetMyBusinessesAsync(Guid userId, CancellationToken ct) =>
        ApiResponse<IReadOnlyList<CommunityBusinessDto>>.SuccessResponse((await context.CommunityBusinesses.AsNoTracking().Where(item => item.OwnerUserId == userId).OrderByDescending(item => item.UpdatedAtUtc).ToListAsync(ct)).Select(MapBusiness).ToList());

    public async Task<ApiResponse<CommunityBusinessDto>> SaveBusinessAsync(Guid? id, Guid userId, UpsertCommunityBusinessRequest request, CancellationToken ct)
    {
        if (!ValidHttps(request.WebsiteUrl, true) || !ValidHttps(request.LogoUrl, true)) return ApiResponse<CommunityBusinessDto>.ErrorResponse("Website and logo links must use https");
        var item = id.HasValue ? await context.CommunityBusinesses.SingleOrDefaultAsync(value => value.Id == id && value.OwnerUserId == userId, ct) : null;
        if (id.HasValue && item == null) return ApiResponse<CommunityBusinessDto>.ErrorResponse("Business not found");
        if (item == null) { item = new CommunityBusiness { OwnerUserId = userId }; context.CommunityBusinesses.Add(item); }
        item.Name = request.Name.Trim(); item.NameEn = Trim(request.NameEn); item.Category = request.Category.Trim(); item.Description = request.Description.Trim(); item.DescriptionEn = Trim(request.DescriptionEn);
        item.Services = Trim(request.Services); item.ServicesEn = Trim(request.ServicesEn); item.ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(); item.ContactPhone = Trim(request.ContactPhone);
        item.WebsiteUrl = Trim(request.WebsiteUrl); item.LogoUrl = Trim(request.LogoUrl); item.City = Trim(request.City); item.Province = Trim(request.Province); item.ServiceRegions = Trim(request.ServiceRegions);
        item.Status = CommunityProgramStatuses.Submitted; item.ReviewNotes = null; item.ReviewedAtUtc = null; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<CommunityBusinessDto>.SuccessResponse(MapBusiness(item));
    }

    public async Task<ApiResponse<CommunityBusinessDto>> ReviewBusinessAsync(Guid id, ReviewCommunityBusinessRequest request, CancellationToken ct)
    {
        var status = ReviewStatuses.FirstOrDefault(value => value.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (status == null) return ApiResponse<CommunityBusinessDto>.ErrorResponse("Invalid review status");
        var item = await context.CommunityBusinesses.Include(value => value.OwnerUser).SingleOrDefaultAsync(value => value.Id == id, ct);
        if (item == null) return ApiResponse<CommunityBusinessDto>.ErrorResponse("Business not found");
        item.Status = status; item.IsFeatured = status == CommunityProgramStatuses.Approved && request.IsFeatured; item.ReviewNotes = Trim(request.ReviewNotes); item.ReviewedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        if (item.OwnerUser?.Email is { Length: > 0 } recipient) emailOutbox.Enqueue(recipient, status == CommunityProgramStatuses.Approved ? "Votre fiche entreprise est publiée / Your business listing is live" : "Mise à jour de votre fiche entreprise / Business listing update", $"<p>{Encode(item.Name)}</p><p>Statut / Status: <strong>{status}</strong></p>", nameof(CommunityBusiness), item.Id);
        await context.SaveChangesAsync(ct); return ApiResponse<CommunityBusinessDto>.SuccessResponse(MapBusiness(item));
    }

    public async Task<ApiResponse<NewcomerJourneyDto?>> GetJourneyAsync(Guid userId, CancellationToken ct)
    {
        var item = await context.NewcomerJourneys.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == userId, ct);
        return ApiResponse<NewcomerJourneyDto?>.SuccessResponse(item == null ? null : MapJourney(item));
    }

    public async Task<ApiResponse<NewcomerJourneyDto>> SaveJourneyAsync(Guid userId, UpsertNewcomerJourneyRequest request, CancellationToken ct)
    {
        if (request.PreferredLanguage is not ("fr" or "en")) return ApiResponse<NewcomerJourneyDto>.ErrorResponse("Language must be fr or en");
        var steps = (request.CompletedSteps ?? []).Where(JourneySteps.Contains).Distinct().ToList();
        var item = await context.NewcomerJourneys.SingleOrDefaultAsync(value => value.UserId == userId, ct);
        if (item == null) { item = new NewcomerJourney { UserId = userId }; context.NewcomerJourneys.Add(item); }
        item.ArrivalDate = request.ArrivalDate; item.City = Trim(request.City); item.Province = Trim(request.Province); item.PreferredLanguage = request.PreferredLanguage;
        item.NeedsJson = Json(request.Needs ?? []); item.CompletedStepsJson = Json(steps); item.MentorRequested = request.MentorRequested; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<NewcomerJourneyDto>.SuccessResponse(MapJourney(item));
    }

    public async Task<ApiResponse<FamilyHouseholdDto?>> GetHouseholdAsync(Guid userId, CancellationToken ct)
    {
        var item = await context.FamilyHouseholds.AsNoTracking().Include(value => value.Members).SingleOrDefaultAsync(value => value.OwnerUserId == userId, ct);
        return ApiResponse<FamilyHouseholdDto?>.SuccessResponse(item == null ? null : MapHousehold(item));
    }

    public async Task<ApiResponse<FamilyHouseholdDto>> SaveHouseholdAsync(Guid userId, UpsertFamilyHouseholdRequest request, CancellationToken ct)
    {
        var item = await context.FamilyHouseholds.Include(value => value.Members).SingleOrDefaultAsync(value => value.OwnerUserId == userId, ct);
        if (item == null) { item = new FamilyHousehold { OwnerUserId = userId }; context.FamilyHouseholds.Add(item); }
        item.HouseholdName = request.HouseholdName.Trim(); item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<FamilyHouseholdDto>.SuccessResponse(MapHousehold(item));
    }

    public async Task<ApiResponse<FamilyHouseholdDto>> AddFamilyMemberAsync(Guid userId, AddFamilyMemberRequest request, CancellationToken ct)
    {
        var household = await context.FamilyHouseholds.Include(value => value.Members).SingleOrDefaultAsync(value => value.OwnerUserId == userId, ct);
        if (household == null) return ApiResponse<FamilyHouseholdDto>.ErrorResponse("Create the family membership first");
        if (household.Members.Count(value => value.Status == CommunityProgramStatuses.Active) >= 8) return ApiResponse<FamilyHouseholdDto>.ErrorResponse("A family membership can include up to 8 people");
        var member = new FamilyHouseholdMember { HouseholdId = household.Id, Household = household, FullName = request.FullName.Trim(), Relationship = request.Relationship.Trim(), Email = Trim(request.Email)?.ToLowerInvariant(), BirthDate = request.BirthDate };
        context.FamilyHouseholdMembers.Add(member);
        household.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); return ApiResponse<FamilyHouseholdDto>.SuccessResponse(MapHousehold(household));
    }

    public async Task<ApiResponse<FamilyHouseholdDto>> RemoveFamilyMemberAsync(Guid userId, Guid memberId, CancellationToken ct)
    {
        var household = await context.FamilyHouseholds.Include(value => value.Members).SingleOrDefaultAsync(value => value.OwnerUserId == userId, ct);
        var member = household?.Members.SingleOrDefault(value => value.Id == memberId);
        if (household == null || member == null) return ApiResponse<FamilyHouseholdDto>.ErrorResponse("Family member not found");
        member.Status = CommunityProgramStatuses.Cancelled; household.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); return ApiResponse<FamilyHouseholdDto>.SuccessResponse(MapHousehold(household));
    }

    public async Task<ApiResponse<IReadOnlyList<AppointmentSlotDto>>> GetAvailableSlotsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var items = await context.AppointmentSlots.AsNoTracking().Include(item => item.Offering).Include(item => item.Bookings)
            .Where(item => !item.IsCancelled && item.StartsAtUtc > now && item.Offering!.IsActive).OrderBy(item => item.StartsAtUtc).Take(150).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<AppointmentSlotDto>>.SuccessResponse(items.Where(item => Available(item) > 0).Select(MapSlot).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<AppointmentBookingDto>>> GetMyBookingsAsync(Guid userId, CancellationToken ct) =>
        ApiResponse<IReadOnlyList<AppointmentBookingDto>>.SuccessResponse((await context.AppointmentBookings.AsNoTracking().Include(item => item.Slot).ThenInclude(item => item!.Offering).Where(item => item.UserId == userId).OrderByDescending(item => item.Slot!.StartsAtUtc).ToListAsync(ct)).Select(MapBooking).ToList());

    public async Task<ApiResponse<AppointmentBookingDto>> BookAsync(Guid userId, CreateAppointmentBookingRequest request, CancellationToken ct)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var slot = await context.AppointmentSlots.Include(item => item.Offering).Include(item => item.Bookings).SingleOrDefaultAsync(item => item.Id == request.SlotId, ct);
        if (slot == null || slot.IsCancelled || slot.StartsAtUtc <= DateTime.UtcNow || slot.Offering?.IsActive != true) return ApiResponse<AppointmentBookingDto>.ErrorResponse("Appointment slot is not available");
        var item = slot.Bookings.SingleOrDefault(value => value.UserId == userId);
        if (item?.Status == CommunityProgramStatuses.Active) return ApiResponse<AppointmentBookingDto>.ErrorResponse("You already booked this appointment");
        if (Available(slot) <= 0) return ApiResponse<AppointmentBookingDto>.ErrorResponse("Appointment slot is full");
        if (item == null)
        {
            item = new AppointmentBooking { SlotId = slot.Id, UserId = userId };
            context.AppointmentBookings.Add(item);
        }
        item.Reason = Trim(request.Reason);
        item.Status = CommunityProgramStatuses.Active;
        item.CreatedAtUtc = DateTime.UtcNow;
        item.CancelledAtUtc = null;
        var user = await context.Users.AsNoTracking().SingleAsync(value => value.Id == userId, ct);
        emailOutbox.Enqueue(user.Email, "Rendez-vous confirmé / Appointment confirmed", $"<p><strong>{Encode(slot.Offering.Title)}</strong></p><p>{slot.StartsAtUtc:u}</p>", nameof(AppointmentBooking), item.Id);
        await context.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        item.Slot = slot;
        return ApiResponse<AppointmentBookingDto>.SuccessResponse(MapBooking(item));
    }

    public async Task<ApiResponse<AppointmentBookingDto>> CancelBookingAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var item = await context.AppointmentBookings.Include(value => value.Slot).ThenInclude(value => value!.Offering).SingleOrDefaultAsync(value => value.Id == id && value.UserId == userId, ct);
        if (item == null) return ApiResponse<AppointmentBookingDto>.ErrorResponse("Appointment not found");
        item.Status = CommunityProgramStatuses.Cancelled; item.CancelledAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); return ApiResponse<AppointmentBookingDto>.SuccessResponse(MapBooking(item));
    }

    public async Task<ApiResponse<IReadOnlyList<PartnerBenefitDto>>> GetBenefitsAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var items = await context.PartnerBenefits.AsNoTracking().Include(item => item.Partner).Include(item => item.Claims)
            .Where(item => item.IsActive && item.Partner!.IsActive && (!item.StartsAtUtc.HasValue || item.StartsAtUtc <= now) && (!item.EndsAtUtc.HasValue || item.EndsAtUtc >= now)).OrderBy(item => item.Partner!.DisplayOrder).ThenBy(item => item.Title).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<PartnerBenefitDto>>.SuccessResponse(items.Select(item => MapBenefit(item, userId)).ToList());
    }

    public async Task<ApiResponse<PartnerBenefitDto>> ClaimBenefitAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var item = await context.PartnerBenefits.Include(value => value.Partner).Include(value => value.Claims).SingleOrDefaultAsync(value => value.Id == id && value.IsActive, ct);
        if (item == null || item.EndsAtUtc < DateTime.UtcNow || item.StartsAtUtc > DateTime.UtcNow) return ApiResponse<PartnerBenefitDto>.ErrorResponse("Benefit is not available");
        var claim = item.Claims.SingleOrDefault(value => value.UserId == userId);
        if (claim == null)
        {
            if (item.MaxClaims.HasValue && item.Claims.Count(value => value.Status == CommunityProgramStatuses.Active) >= item.MaxClaims) return ApiResponse<PartnerBenefitDto>.ErrorResponse("Benefit claim limit reached");
            claim = new PartnerBenefitClaim { BenefitId = item.Id, Benefit = item, UserId = userId, RedemptionCode = string.IsNullOrWhiteSpace(item.SharedCode) ? $"HCBE-{RandomNumberGenerator.GetHexString(8)}" : item.SharedCode.Trim().ToUpperInvariant() };
            context.PartnerBenefitClaims.Add(claim);
            await context.SaveChangesAsync(ct);
        }
        return ApiResponse<PartnerBenefitDto>.SuccessResponse(MapBenefit(item, userId));
    }

    public async Task<ApiResponse<IReadOnlyList<GrantApplicationDto>>> GetMyGrantApplicationsAsync(Guid userId, CancellationToken ct) =>
        ApiResponse<IReadOnlyList<GrantApplicationDto>>.SuccessResponse((await context.GrantApplications.AsNoTracking().Include(item => item.GrantProgram).Where(item => item.UserId == userId).OrderByDescending(item => item.SubmittedAtUtc).ToListAsync(ct)).Select(MapGrant).ToList());

    public async Task<ApiResponse<GrantApplicationDto>> ApplyForGrantAsync(Guid userId, CreateGrantApplicationRequest request, CancellationToken ct)
    {
        if ((request.Documents ?? []).Any(value => !ValidHttps(value))) return ApiResponse<GrantApplicationDto>.ErrorResponse("Document links must use https");
        var program = await context.GrantPrograms.SingleOrDefaultAsync(value => value.Id == request.GrantProgramId && value.IsActive, ct);
        if (program == null) return ApiResponse<GrantApplicationDto>.ErrorResponse("Grant program not found");
        if (await context.GrantApplications.AnyAsync(value => value.GrantProgramId == program.Id && value.UserId == userId, ct)) return ApiResponse<GrantApplicationDto>.ErrorResponse("An application already exists for this program");
        var item = new GrantApplication { GrantProgramId = program.Id, UserId = userId, ApplicantName = request.ApplicantName.Trim(), ApplicantEmail = request.ApplicantEmail.Trim().ToLowerInvariant(), Statement = request.Statement.Trim(), AnswersJson = Json(request.Answers ?? new Dictionary<string, string>()), DocumentsJson = Json(request.Documents ?? []) };
        context.GrantApplications.Add(item); await context.SaveChangesAsync(ct); item.GrantProgram = program; return ApiResponse<GrantApplicationDto>.SuccessResponse(MapGrant(item));
    }

    public async Task<ApiResponse<GrantApplicationDto>> WithdrawGrantApplicationAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var item = await context.GrantApplications.Include(value => value.GrantProgram).SingleOrDefaultAsync(value => value.Id == id && value.UserId == userId, ct);
        if (item == null || item.Status != CommunityProgramStatuses.Submitted) return ApiResponse<GrantApplicationDto>.ErrorResponse("Application cannot be withdrawn");
        item.Status = CommunityProgramStatuses.Withdrawn; item.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); return ApiResponse<GrantApplicationDto>.SuccessResponse(MapGrant(item));
    }

    public async Task<ApiResponse<IReadOnlyList<SponsorshipPackageDto>>> GetSponsorshipPackagesAsync(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<SponsorshipPackageDto>>.SuccessResponse((await context.SponsorshipPackages.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ToListAsync(ct)).Select(MapPackage).ToList());
    public async Task<ApiResponse<IReadOnlyList<SponsorshipRequestDto>>> GetMySponsorshipRequestsAsync(Guid userId, CancellationToken ct) =>
        ApiResponse<IReadOnlyList<SponsorshipRequestDto>>.SuccessResponse((await context.SponsorshipRequests.AsNoTracking().Include(item => item.Package).Where(item => item.UserId == userId).OrderByDescending(item => item.CreatedAtUtc).ToListAsync(ct)).Select(MapSponsorship).ToList());

    public async Task<ApiResponse<SponsorshipRequestDto>> RequestSponsorshipAsync(Guid userId, CreateSponsorshipRequest request, CancellationToken ct)
    {
        var package = request.PackageId.HasValue ? await context.SponsorshipPackages.SingleOrDefaultAsync(value => value.Id == request.PackageId && value.IsActive, ct) : null;
        if (request.PackageId.HasValue && package == null) return ApiResponse<SponsorshipRequestDto>.ErrorResponse("Sponsorship package not found");
        if (!request.Currency.Equals("cad", StringComparison.OrdinalIgnoreCase)) return ApiResponse<SponsorshipRequestDto>.ErrorResponse("Only CAD sponsorships are supported");
        var item = new SponsorshipRequest { UserId = userId, PackageId = package?.Id, Package = package, OrganizationName = request.OrganizationName.Trim(), ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(), Objective = request.Objective.Trim(), ProposedAmountCents = package?.AmountCents ?? request.ProposedAmountCents, Currency = "cad" };
        context.SponsorshipRequests.Add(item); await context.SaveChangesAsync(ct); return ApiResponse<SponsorshipRequestDto>.SuccessResponse(MapSponsorship(item));
    }

    public async Task<ApiResponse<IReadOnlyList<AnnualCommunityReportDto>>> GetPublishedReportsAsync(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<AnnualCommunityReportDto>>.SuccessResponse((await context.AnnualCommunityReports.AsNoTracking().Where(item => item.Status == CommunityProgramStatuses.Published).OrderByDescending(item => item.Year).ToListAsync(ct)).Select(MapReport).ToList());

    public async Task<ApiResponse<CommunityProgramsAdminOverviewDto>> GetAdminOverviewAsync(CancellationToken ct)
    {
        await SeedAutomationRulesAsync(ct);
        var businesses = (await context.CommunityBusinesses.AsNoTracking().OrderByDescending(item => item.UpdatedAtUtc).ToListAsync(ct)).Select(MapBusiness).ToList();
        var offerings = (await context.AppointmentOfferings.AsNoTracking().OrderBy(item => item.Title).ToListAsync(ct)).Select(MapOffering).ToList();
        var slotsRaw = await context.AppointmentSlots.AsNoTracking().Include(item => item.Offering).Include(item => item.Bookings).OrderByDescending(item => item.StartsAtUtc).Take(250).ToListAsync(ct);
        var bookings = (await context.AppointmentBookings.AsNoTracking().Include(item => item.Slot).ThenInclude(item => item!.Offering).OrderByDescending(item => item.CreatedAtUtc).Take(250).ToListAsync(ct)).Select(MapBooking).ToList();
        var benefitsRaw = await context.PartnerBenefits.AsNoTracking().Include(item => item.Partner).Include(item => item.Claims).OrderByDescending(item => item.UpdatedAtUtc).ToListAsync(ct);
        var grants = (await context.GrantApplications.AsNoTracking().Include(item => item.GrantProgram).OrderByDescending(item => item.SubmittedAtUtc).ToListAsync(ct)).Select(MapGrant).ToList();
        var packages = (await context.SponsorshipPackages.AsNoTracking().OrderBy(item => item.DisplayOrder).ToListAsync(ct)).Select(MapPackage).ToList();
        var sponsorships = (await context.SponsorshipRequests.AsNoTracking().Include(item => item.Package).OrderByDescending(item => item.CreatedAtUtc).ToListAsync(ct)).Select(MapSponsorship).ToList();
        var reports = (await context.AnnualCommunityReports.AsNoTracking().OrderByDescending(item => item.Year).ToListAsync(ct)).Select(MapReport).ToList();
        var rules = (await context.OperationalAutomationRules.AsNoTracking().OrderBy(item => item.Name).ToListAsync(ct)).Select(MapRule).ToList();
        return ApiResponse<CommunityProgramsAdminOverviewDto>.SuccessResponse(new(businesses, offerings, slotsRaw.Select(MapSlot).ToList(), bookings, benefitsRaw.Select(item => MapBenefit(item, null)).ToList(), grants, packages, sponsorships, reports, rules));
    }

    public async Task<ApiResponse<AppointmentOfferingDto>> SaveOfferingAsync(Guid? id, UpsertAppointmentOfferingRequest request, CancellationToken ct)
    {
        if (request.Mode is not ("Online" or "InPerson" or "Phone")) return ApiResponse<AppointmentOfferingDto>.ErrorResponse("Invalid appointment mode");
        var item = id.HasValue ? await context.AppointmentOfferings.FindAsync([id.Value], ct) : null;
        if (id.HasValue && item == null) return ApiResponse<AppointmentOfferingDto>.ErrorResponse("Offering not found");
        if (item == null) { item = new AppointmentOffering(); context.AppointmentOfferings.Add(item); }
        item.Title = request.Title.Trim(); item.TitleEn = Trim(request.TitleEn); item.Description = request.Description.Trim(); item.DescriptionEn = Trim(request.DescriptionEn); item.Category = request.Category.Trim(); item.Mode = request.Mode; item.Location = Trim(request.Location); item.LocationEn = Trim(request.LocationEn); item.DurationMinutes = request.DurationMinutes; item.IsActive = request.IsActive; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<AppointmentOfferingDto>.SuccessResponse(MapOffering(item));
    }

    public async Task<ApiResponse<AppointmentSlotDto>> CreateSlotAsync(CreateAppointmentSlotRequest request, CancellationToken ct)
    {
        var offering = await context.AppointmentOfferings.FindAsync([request.OfferingId], ct);
        if (offering == null || request.StartsAtUtc <= DateTime.UtcNow || request.EndsAtUtc <= request.StartsAtUtc) return ApiResponse<AppointmentSlotDto>.ErrorResponse("Invalid appointment slot");
        if (await context.AppointmentSlots.AnyAsync(item => item.OfferingId == request.OfferingId && !item.IsCancelled && item.StartsAtUtc < request.EndsAtUtc && item.EndsAtUtc > request.StartsAtUtc, ct)) return ApiResponse<AppointmentSlotDto>.ErrorResponse("Appointment slot overlaps an existing slot");
        var item = new AppointmentSlot { OfferingId = offering.Id, Offering = offering, StartsAtUtc = request.StartsAtUtc.ToUniversalTime(), EndsAtUtc = request.EndsAtUtc.ToUniversalTime(), Capacity = request.Capacity }; context.AppointmentSlots.Add(item); await context.SaveChangesAsync(ct); return ApiResponse<AppointmentSlotDto>.SuccessResponse(MapSlot(item));
    }

    public async Task<ApiResponse<PartnerBenefitDto>> SaveBenefitAsync(Guid? id, UpsertPartnerBenefitRequest request, CancellationToken ct)
    {
        if (request.EndsAtUtc <= request.StartsAtUtc) return ApiResponse<PartnerBenefitDto>.ErrorResponse("Benefit end must be after its start");
        var partner = await context.Partners.SingleOrDefaultAsync(value => value.Id == request.PartnerId, ct); if (partner == null) return ApiResponse<PartnerBenefitDto>.ErrorResponse("Partner not found");
        var item = id.HasValue ? await context.PartnerBenefits.Include(value => value.Partner).Include(value => value.Claims).SingleOrDefaultAsync(value => value.Id == id, ct) : null;
        if (id.HasValue && item == null) return ApiResponse<PartnerBenefitDto>.ErrorResponse("Benefit not found");
        if (item == null) { item = new PartnerBenefit { PartnerId = partner.Id, Partner = partner }; context.PartnerBenefits.Add(item); }
        item.Title = request.Title.Trim(); item.TitleEn = Trim(request.TitleEn); item.Description = request.Description.Trim(); item.DescriptionEn = Trim(request.DescriptionEn); item.Terms = Trim(request.Terms); item.TermsEn = Trim(request.TermsEn); item.RedemptionInstructions = Trim(request.RedemptionInstructions); item.RedemptionInstructionsEn = Trim(request.RedemptionInstructionsEn); item.SharedCode = Trim(request.SharedCode); item.StartsAtUtc = request.StartsAtUtc; item.EndsAtUtc = request.EndsAtUtc; item.MaxClaims = request.MaxClaims; item.IsActive = request.IsActive; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<PartnerBenefitDto>.SuccessResponse(MapBenefit(item, null));
    }

    public async Task<ApiResponse<GrantApplicationDto>> ReviewGrantApplicationAsync(Guid id, ReviewGrantApplicationRequest request, CancellationToken ct)
    {
        var status = ReviewStatuses.FirstOrDefault(value => value.Equals(request.Status, StringComparison.OrdinalIgnoreCase)); if (status == null) return ApiResponse<GrantApplicationDto>.ErrorResponse("Invalid review status");
        var item = await context.GrantApplications.Include(value => value.GrantProgram).SingleOrDefaultAsync(value => value.Id == id, ct); if (item == null) return ApiResponse<GrantApplicationDto>.ErrorResponse("Application not found");
        item.Status = status; item.AdminNotes = Trim(request.AdminNotes); item.ReviewedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        emailOutbox.Enqueue(item.ApplicantEmail, "Décision concernant votre bourse / Grant application decision", $"<p>{Encode(item.GrantProgram?.Title)}</p><p>Statut / Status: <strong>{status}</strong></p>", nameof(GrantApplication), item.Id);
        await context.SaveChangesAsync(ct); return ApiResponse<GrantApplicationDto>.SuccessResponse(MapGrant(item));
    }

    public async Task<ApiResponse<SponsorshipPackageDto>> SaveSponsorshipPackageAsync(Guid? id, UpsertSponsorshipPackageRequest request, CancellationToken ct)
    {
        if (!request.Currency.Equals("cad", StringComparison.OrdinalIgnoreCase)) return ApiResponse<SponsorshipPackageDto>.ErrorResponse("Only CAD sponsorship packages are supported");
        var item = id.HasValue ? await context.SponsorshipPackages.FindAsync([id.Value], ct) : null; if (id.HasValue && item == null) return ApiResponse<SponsorshipPackageDto>.ErrorResponse("Package not found");
        if (item == null) { item = new SponsorshipPackage(); context.SponsorshipPackages.Add(item); }
        item.Title = request.Title.Trim(); item.TitleEn = Trim(request.TitleEn); item.Description = request.Description.Trim(); item.DescriptionEn = Trim(request.DescriptionEn); item.DeliverablesJson = Json(request.Deliverables ?? []); item.AmountCents = request.AmountCents; item.Currency = "cad"; item.IsActive = request.IsActive; item.DisplayOrder = request.DisplayOrder; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<SponsorshipPackageDto>.SuccessResponse(MapPackage(item));
    }

    public async Task<ApiResponse<SponsorshipRequestDto>> ReviewSponsorshipAsync(Guid id, ReviewSponsorshipRequest request, CancellationToken ct)
    {
        var status = ReviewStatuses.FirstOrDefault(value => value.Equals(request.Status, StringComparison.OrdinalIgnoreCase)); if (status == null) return ApiResponse<SponsorshipRequestDto>.ErrorResponse("Invalid review status");
        var item = await context.SponsorshipRequests.Include(value => value.Package).SingleOrDefaultAsync(value => value.Id == id, ct); if (item == null) return ApiResponse<SponsorshipRequestDto>.ErrorResponse("Sponsorship request not found");
        item.Status = status; item.Notes = Trim(request.Notes); item.ReviewedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        emailOutbox.Enqueue(item.ContactEmail, "Demande de commandite / Sponsorship request", $"<p>{Encode(item.OrganizationName)}</p><p>Statut / Status: <strong>{status}</strong></p>", nameof(SponsorshipRequest), item.Id);
        await context.SaveChangesAsync(ct); return ApiResponse<SponsorshipRequestDto>.SuccessResponse(MapSponsorship(item));
    }

    public async Task<ApiResponse<AnnualCommunityReportDto>> GenerateAnnualReportAsync(int year, CancellationToken ct)
    {
        if (year < 2020 || year > DateTime.UtcNow.Year) return ApiResponse<AnnualCommunityReportDto>.ErrorResponse("Invalid report year");
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc); var end = start.AddYears(1);
        var metrics = new Dictionary<string, decimal> {
            ["newMembers"] = await context.Members.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end, ct),
            ["eventRegistrations"] = await context.EventRegistrations.CountAsync(item => item.RegisteredAt >= start && item.RegisteredAt < end, ct),
            ["volunteerHours"] = (await context.VolunteerTimeEntries.Where(item => item.Status == "Approved" && item.CreatedAt >= start && item.CreatedAt < end).Select(item => item.Hours).ToListAsync(ct)).Sum(),
            ["serviceRequests"] = await context.ServiceCases.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end, ct),
            ["appointments"] = await context.AppointmentBookings.CountAsync(item => item.Status == CommunityProgramStatuses.Active && item.CreatedAtUtc >= start && item.CreatedAtUtc < end, ct),
            ["businessesPublished"] = await context.CommunityBusinesses.CountAsync(item => item.Status == CommunityProgramStatuses.Approved && item.ReviewedAtUtc >= start && item.ReviewedAtUtc < end, ct),
            ["grantApplications"] = await context.GrantApplications.CountAsync(item => item.SubmittedAtUtc >= start && item.SubmittedAtUtc < end, ct)
        };
        var item = await context.AnnualCommunityReports.SingleOrDefaultAsync(value => value.Year == year, ct);
        if (item == null) { item = new AnnualCommunityReport { Year = year }; context.AnnualCommunityReports.Add(item); }
        item.Title = $"Rapport annuel {year}"; item.TitleEn = $"{year} annual report"; item.Summary = $"Portrait transparent de l’impact communautaire du HCBE Canada en {year}."; item.SummaryEn = $"A transparent overview of HCBE Canada’s community impact in {year}."; item.MetricsJson = Json(metrics); item.Status = CommunityProgramStatuses.Draft; item.GeneratedAtUtc = DateTime.UtcNow; item.PublishedAtUtc = null;
        await context.SaveChangesAsync(ct); return ApiResponse<AnnualCommunityReportDto>.SuccessResponse(MapReport(item));
    }

    public async Task<ApiResponse<AnnualCommunityReportDto>> PublishAnnualReportAsync(Guid id, CancellationToken ct)
    {
        var item = await context.AnnualCommunityReports.FindAsync([id], ct); if (item == null) return ApiResponse<AnnualCommunityReportDto>.ErrorResponse("Report not found");
        item.Status = CommunityProgramStatuses.Published; item.PublishedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); return ApiResponse<AnnualCommunityReportDto>.SuccessResponse(MapReport(item));
    }

    public async Task<ApiResponse<AutomationRuleDto>> UpdateAutomationRuleAsync(Guid id, UpdateAutomationRuleRequest request, CancellationToken ct)
    {
        if (request.Cadence is not ("Daily" or "Weekly" or "Monthly" or "Yearly")) return ApiResponse<AutomationRuleDto>.ErrorResponse("Invalid cadence");
        var item = await context.OperationalAutomationRules.FindAsync([id], ct); if (item == null) return ApiResponse<AutomationRuleDto>.ErrorResponse("Automation rule not found");
        item.IsEnabled = request.IsEnabled; item.Cadence = request.Cadence; item.NextRunAtUtc = NextRun(DateTime.UtcNow, request.Cadence); item.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); return ApiResponse<AutomationRuleDto>.SuccessResponse(MapRule(item));
    }

    public async Task<ApiResponse<IReadOnlyList<AutomationRuleDto>>> RunDueAutomationsAsync(bool force, CancellationToken ct)
    {
        await SeedAutomationRulesAsync(ct); var now = DateTime.UtcNow;
        var rules = await context.OperationalAutomationRules.Where(item => item.IsEnabled && (force || item.NextRunAtUtc == null || item.NextRunAtUtc <= now)).ToListAsync(ct);
        foreach (var rule in rules)
        {
            rule.LastRunAtUtc = now; rule.LastStatus = "Succeeded"; rule.LastSummary = rule.Key switch { "annual-report-draft" => "Annual report snapshot refreshed", "appointment-capacity" => "Appointment capacity reviewed", "grant-follow-up" => "Grant application queue reviewed", _ => "Community operations reviewed" }; rule.NextRunAtUtc = NextRun(now, rule.Cadence); rule.UpdatedAtUtc = now;
            if (rule.Key == "annual-report-draft") await GenerateAnnualReportAsync(now.Year, ct);
        }
        await context.SaveChangesAsync(ct); return ApiResponse<IReadOnlyList<AutomationRuleDto>>.SuccessResponse((await context.OperationalAutomationRules.AsNoTracking().OrderBy(item => item.Name).ToListAsync(ct)).Select(MapRule).ToList());
    }

    private async Task SeedAutomationRulesAsync(CancellationToken ct)
    {
        if (await context.OperationalAutomationRules.AnyAsync(ct)) return;
        context.OperationalAutomationRules.AddRange(
            new OperationalAutomationRule { Key = "annual-report-draft", Name = "Brouillon du rapport annuel", NameEn = "Annual report draft", Cadence = "Monthly", NextRunAtUtc = DateTime.UtcNow },
            new OperationalAutomationRule { Key = "appointment-capacity", Name = "Surveillance des rendez-vous", NameEn = "Appointment capacity monitoring", Cadence = "Daily", NextRunAtUtc = DateTime.UtcNow },
            new OperationalAutomationRule { Key = "grant-follow-up", Name = "Suivi des demandes de bourse", NameEn = "Grant application follow-up", Cadence = "Weekly", NextRunAtUtc = DateTime.UtcNow });
        await context.SaveChangesAsync(ct);
    }

    private static CommunityBusinessDto MapBusiness(CommunityBusiness item) => new(item.Id, item.Name, item.NameEn, item.Category, item.Description, item.DescriptionEn, item.Services, item.ServicesEn, item.ContactEmail, item.ContactPhone, item.WebsiteUrl, item.LogoUrl, item.City, item.Province, item.ServiceRegions, item.Status, item.IsFeatured, item.ReviewNotes, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static NewcomerJourneyDto MapJourney(NewcomerJourney item) { var completed = ReadList(item.CompletedStepsJson); return new(item.Id, item.ArrivalDate, item.City, item.Province, item.PreferredLanguage, ReadList(item.NeedsJson), completed, item.MentorRequested, (int)Math.Round(completed.Count * 100d / JourneySteps.Length), item.UpdatedAtUtc); }
    private static FamilyHouseholdDto MapHousehold(FamilyHousehold item) => new(item.Id, item.HouseholdName, item.Status, item.Members.OrderBy(value => value.CreatedAtUtc).Select(value => new FamilyHouseholdMemberDto(value.Id, value.FullName, value.Relationship, value.Email, value.BirthDate, value.Status, value.CreatedAtUtc)).ToList(), item.UpdatedAtUtc);
    private static AppointmentOfferingDto MapOffering(AppointmentOffering item) => new(item.Id, item.Title, item.TitleEn, item.Description, item.DescriptionEn, item.Category, item.Mode, item.Location, item.LocationEn, item.DurationMinutes, item.IsActive);
    private static AppointmentSlotDto MapSlot(AppointmentSlot item) => new(item.Id, item.OfferingId, item.Offering?.Title ?? string.Empty, item.Offering?.TitleEn, item.StartsAtUtc, item.EndsAtUtc, item.Capacity, Available(item), item.IsCancelled);
    private static AppointmentBookingDto MapBooking(AppointmentBooking item) => new(item.Id, item.SlotId, item.Slot?.Offering?.Title ?? string.Empty, item.Slot?.Offering?.TitleEn, item.Slot?.StartsAtUtc ?? default, item.Slot?.EndsAtUtc ?? default, item.Reason, item.Status, item.CreatedAtUtc);
    private static PartnerBenefitDto MapBenefit(PartnerBenefit item, Guid? userId) { var claim = userId.HasValue ? item.Claims.SingleOrDefault(value => value.UserId == userId) : null; return new(item.Id, item.PartnerId, item.Partner?.Name ?? string.Empty, item.Partner?.LogoUrl, item.Title, item.TitleEn, item.Description, item.DescriptionEn, item.Terms, item.TermsEn, item.RedemptionInstructions, item.RedemptionInstructionsEn, item.StartsAtUtc, item.EndsAtUtc, item.MaxClaims, item.Claims.Count(value => value.Status == CommunityProgramStatuses.Active), item.IsActive, claim != null, claim?.RedemptionCode); }
    private static GrantApplicationDto MapGrant(GrantApplication item) => new(item.Id, item.GrantProgramId, item.GrantProgram?.Title ?? string.Empty, item.GrantProgram?.TitleEn, item.ApplicantName, item.ApplicantEmail, item.Statement, ReadDictionary(item.AnswersJson), ReadList(item.DocumentsJson), item.Status, item.AdminNotes, item.SubmittedAtUtc, item.UpdatedAtUtc);
    private static SponsorshipPackageDto MapPackage(SponsorshipPackage item) => new(item.Id, item.Title, item.TitleEn, item.Description, item.DescriptionEn, ReadList(item.DeliverablesJson), item.AmountCents, item.Currency, item.IsActive, item.DisplayOrder);
    private static SponsorshipRequestDto MapSponsorship(SponsorshipRequest item) => new(item.Id, item.PackageId, item.Package?.Title, item.OrganizationName, item.ContactEmail, item.Objective, item.Notes, item.ProposedAmountCents, item.Currency, item.Status, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static AnnualCommunityReportDto MapReport(AnnualCommunityReport item) => new(item.Id, item.Year, item.Title, item.TitleEn, item.Summary, item.SummaryEn, ReadDecimalDictionary(item.MetricsJson), item.Status, item.GeneratedAtUtc, item.PublishedAtUtc);
    private static AutomationRuleDto MapRule(OperationalAutomationRule item) => new(item.Id, item.Key, item.Name, item.NameEn, item.Cadence, item.IsEnabled, item.LastRunAtUtc, item.NextRunAtUtc, item.LastStatus, item.LastSummary);
    private static int Available(AppointmentSlot item) => Math.Max(0, item.Capacity - item.Bookings.Count(value => value.Status == CommunityProgramStatuses.Active));
    private static DateTime NextRun(DateTime now, string cadence) => cadence switch { "Daily" => now.AddDays(1), "Weekly" => now.AddDays(7), "Monthly" => now.AddMonths(1), "Yearly" => now.AddYears(1), _ => now.AddDays(1) };
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static List<string> ReadList(string value) { try { return JsonSerializer.Deserialize<List<string>>(value) ?? []; } catch { return []; } }
    private static Dictionary<string, string> ReadDictionary(string value) { try { return JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? []; } catch { return []; } }
    private static Dictionary<string, decimal> ReadDecimalDictionary(string value) { try { return JsonSerializer.Deserialize<Dictionary<string, decimal>>(value) ?? []; } catch { return []; } }
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidHttps(string? value, bool optional = false) => optional && string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    private static string Encode(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
