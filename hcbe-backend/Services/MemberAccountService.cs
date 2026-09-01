using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class MemberAccountService : IMemberAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailOutbox? _emailOutbox;
    private readonly IEmailTemplateRenderer? _emailTemplates;
    private readonly IConfiguration? _configuration;

    public MemberAccountService(
        ApplicationDbContext context,
        IEmailOutbox? emailOutbox = null,
        IEmailTemplateRenderer? emailTemplates = null,
        IConfiguration? configuration = null)
    {
        _context = context;
        _emailOutbox = emailOutbox;
        _emailTemplates = emailTemplates;
        _configuration = configuration;
    }

    public async Task<ApiResponse<MemberDto>> GetAsync(Guid userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
        if (user?.MemberId is null)
        {
            return ApiResponse<MemberDto>.ErrorResponse("No member profile is linked to this account");
        }

        var member = await _context.Members.AsNoTracking().FirstOrDefaultAsync(item => item.Id == user.MemberId);
        return member is null
            ? ApiResponse<MemberDto>.ErrorResponse("Member profile not found")
            : ApiResponse<MemberDto>.SuccessResponse(Map(member));
    }

    public async Task<ApiResponse<MemberDto>> UpdateAsync(Guid userId, UpdateMemberAccountRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId);
        if (user?.MemberId is null)
        {
            return ApiResponse<MemberDto>.ErrorResponse("No member profile is linked to this account");
        }

        var member = await _context.Members.FindAsync(user.MemberId.Value);
        if (member is null)
        {
            return ApiResponse<MemberDto>.ErrorResponse("Member profile not found");
        }

        var profileWasComplete = GetMissingRequiredFields(member).Count == 0;

        if (request.FirstName is not null) member.FirstName = request.FirstName.Trim();
        if (request.LastName is not null) member.LastName = request.LastName.Trim();
        if (request.Phone is not null) member.Phone = Normalize(request.Phone);
        if (request.City is not null) member.City = Normalize(request.City);
        if (request.Province is not null) member.Province = Normalize(request.Province);
        if (request.Profession is not null) member.Profession = Normalize(request.Profession);
        if (request.Expertise is not null) member.Expertise = Normalize(request.Expertise);
        if (request.Interests is not null) member.Interests = Normalize(request.Interests);
        if (request.Availability is not null) member.Availability = Normalize(request.Availability);

        var missingFields = GetMissingRequiredFields(member);
        if (missingFields.Count > 0)
        {
            return ApiResponse<MemberDto>.ErrorResponse(
                "Complete all required member profile fields",
                missingFields.Select(field => $"The {field} field is required.").ToList());
        }

        user.FirstName = member.FirstName;
        user.LastName = member.LastName;
        if (!profileWasComplete && _emailOutbox is not null && _emailTemplates is not null)
        {
            var publicUrl = (_configuration?["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var email = _emailTemplates.MemberWelcome(member.FirstName, $"{publicUrl}/espace-membre");
            _emailOutbox.Enqueue(member.Email, email.Subject, email.HtmlBody, nameof(Member), member.Id);
        }
        await _context.SaveChangesAsync();
        return ApiResponse<MemberDto>.SuccessResponse(Map(member));
    }

    private static MemberDto Map(Member member) => new(
        member.Id, member.FirstName, member.LastName, member.Email, member.Phone,
        member.City, member.Province, member.Profession, member.Expertise,
        member.Interests, member.Availability, member.Zone, member.IsAdmin, member.CreatedAt);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> GetMissingRequiredFields(Member member)
    {
        var requiredFields = new (string Name, string? Value)[]
        {
            ("first name", member.FirstName),
            ("last name", member.LastName),
            ("phone", member.Phone),
            ("city", member.City),
            ("province", member.Province),
            ("profession", member.Profession),
            ("professional field", member.Expertise),
            ("membership motivation", member.Interests)
        };

        return requiredFields
            .Where(field => string.IsNullOrWhiteSpace(field.Value))
            .Select(field => field.Name)
            .ToList();
    }
}
