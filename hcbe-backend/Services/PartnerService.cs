using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class PartnerService : IPartnerService
{
    private readonly ApplicationDbContext _context;

    public PartnerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<PartnerDto>>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Partners.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(partner => partner.IsActive);
        }

        var partners = await query
            .OrderBy(partner => partner.DisplayOrder)
            .ThenBy(partner => partner.Name)
            .Select(partner => MapToDto(partner))
            .ToListAsync();

        return ApiResponse<List<PartnerDto>>.SuccessResponse(partners);
    }

    public async Task<ApiResponse<PartnerDto>> GetByIdAsync(Guid id)
    {
        var partner = await _context.Partners.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return partner is null
            ? ApiResponse<PartnerDto>.ErrorResponse("Partner not found")
            : ApiResponse<PartnerDto>.SuccessResponse(MapToDto(partner));
    }

    public async Task<ApiResponse<PartnerDto>> CreateAsync(CreatePartnerRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.Partners.AnyAsync(item => item.Name.ToLower() == name.ToLower()))
        {
            return ApiResponse<PartnerDto>.ErrorResponse("A partner with this name already exists");
        }

        var partner = new Partner
        {
            Name = name,
            NameEn = Normalize(request.NameEn),
            Description = Normalize(request.Description),
            DescriptionEn = Normalize(request.DescriptionEn),
            LogoUrl = Normalize(request.LogoUrl),
            WebsiteUrl = NormalizeUrl(request.WebsiteUrl),
            AltText = Normalize(request.AltText),
            AltTextEn = Normalize(request.AltTextEn),
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();
        return ApiResponse<PartnerDto>.SuccessResponse(MapToDto(partner));
    }

    public async Task<ApiResponse<PartnerDto>> UpdateAsync(Guid id, UpdatePartnerRequest request)
    {
        var partner = await _context.Partners.FindAsync(id);
        if (partner is null)
        {
            return ApiResponse<PartnerDto>.ErrorResponse("Partner not found");
        }

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return ApiResponse<PartnerDto>.ErrorResponse("Partner name is required");
            }
            if (await _context.Partners.AnyAsync(item => item.Id != id && item.Name.ToLower() == name.ToLower()))
            {
                return ApiResponse<PartnerDto>.ErrorResponse("A partner with this name already exists");
            }
            partner.Name = name;
        }

        if (request.NameEn is not null) partner.NameEn = Normalize(request.NameEn);
        if (request.Description is not null) partner.Description = Normalize(request.Description);
        if (request.DescriptionEn is not null) partner.DescriptionEn = Normalize(request.DescriptionEn);
        if (request.LogoUrl is not null) partner.LogoUrl = Normalize(request.LogoUrl);
        if (request.WebsiteUrl is not null) partner.WebsiteUrl = NormalizeUrl(request.WebsiteUrl);
        if (request.AltText is not null) partner.AltText = Normalize(request.AltText);
        if (request.AltTextEn is not null) partner.AltTextEn = Normalize(request.AltTextEn);
        if (request.IsFeatured.HasValue) partner.IsFeatured = request.IsFeatured.Value;
        if (request.IsActive.HasValue) partner.IsActive = request.IsActive.Value;
        if (request.DisplayOrder.HasValue) partner.DisplayOrder = request.DisplayOrder.Value;
        partner.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse<PartnerDto>.SuccessResponse(MapToDto(partner));
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var partner = await _context.Partners.FindAsync(id);
        if (partner is null)
        {
            return ApiResponse.CreateError("Partner not found");
        }

        _context.Partners.Remove(partner);
        await _context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Partner deleted");
    }

    public async Task<ApiResponse<List<PartnerDto>>> ReorderAsync(ReorderPartnersRequest request)
    {
        var requestedIds = request.PartnerIds.Distinct().ToList();
        var partners = await _context.Partners.Where(item => requestedIds.Contains(item.Id)).ToListAsync();
        if (partners.Count != requestedIds.Count)
        {
            return ApiResponse<List<PartnerDto>>.ErrorResponse("One or more partners could not be found");
        }

        for (var index = 0; index < requestedIds.Count; index++)
        {
            var partner = partners.First(item => item.Id == requestedIds[index]);
            partner.DisplayOrder = index;
            partner.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return await GetAllAsync(true);
    }

    private static PartnerDto MapToDto(Partner partner) => new(
        partner.Id,
        partner.Name,
        partner.NameEn,
        partner.Description,
        partner.DescriptionEn,
        partner.LogoUrl,
        partner.WebsiteUrl,
        partner.AltText,
        partner.AltTextEn,
        partner.IsFeatured,
        partner.IsActive,
        partner.DisplayOrder,
        partner.CreatedAt,
        partner.UpdatedAt);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeUrl(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null) return null;
        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.ToString()
            : normalized;
    }
}
