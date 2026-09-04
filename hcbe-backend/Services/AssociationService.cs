using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Helpers;

namespace HcbeApi.Services;

public class AssociationService : IAssociationService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public AssociationService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetAllAsync()
    {
        try
        {
            var associations = await _context.Associations
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var associationDtos = associations.Select(MapToDto).ToList();
            return ApiResponse<List<AssociationDto>>.SuccessResponse(associationDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<AssociationDto>>.ErrorResponse(
                "Failed to retrieve associations",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AssociationDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var association = await _context.Associations.FindAsync(id);
            if (association == null || !association.IsActive)
            {
                return ApiResponse<AssociationDto>.ErrorResponse("Association not found");
            }

            return ApiResponse<AssociationDto>.SuccessResponse(MapToDto(association));
        }
        catch (Exception ex)
        {
            return ApiResponse<AssociationDto>.ErrorResponse(
                "Failed to retrieve association",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetAllForAdminAsync()
    {
        try
        {
            var associations = await _context.Associations
                .OrderBy(a => a.Name)
                .ToListAsync();

            var associationDtos = associations.Select(MapToDto).ToList();
            return ApiResponse<List<AssociationDto>>.SuccessResponse(associationDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<AssociationDto>>.ErrorResponse(
                "Failed to retrieve associations",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AssociationDto>> GetByIdForAdminAsync(Guid id)
    {
        try
        {
            var association = await _context.Associations.FindAsync(id);
            if (association == null)
            {
                return ApiResponse<AssociationDto>.ErrorResponse("Association not found");
            }

            return ApiResponse<AssociationDto>.SuccessResponse(MapToDto(association));
        }
        catch (Exception ex)
        {
            return ApiResponse<AssociationDto>.ErrorResponse(
                "Failed to retrieve association",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AssociationDto>> CreateAsync(CreateAssociationRequest request)
    {
        try
        {
            var association = new Association
            {
                Name = request.Name,
                NameEn = NormalizeOptional(request.NameEn),
                Description = request.Description,
                DescriptionEn = NormalizeOptional(request.DescriptionEn),
                Province = request.Province,
                City = request.City,
                Contact = request.Contact,
                Phone = request.Phone,
                President = request.President,
                MemberCount = request.MemberCount,
                FoundedYear = request.FoundedYear,
                ImageUrl = NormalizeOptional(request.ImageUrl),
                Website = request.Website,
                Domains = request.Domains ?? new List<string>(),
                DomainsEn = request.DomainsEn ?? new List<string>(),
                OrganizationType = NormalizeOrganizationType(request.OrganizationType),
                IsActive = true
            };

            _context.Associations.Add(association);
            await _context.SaveChangesAsync();

            return ApiResponse<AssociationDto>.SuccessResponse(MapToDto(association));
        }
        catch (Exception ex)
        {
            return ApiResponse<AssociationDto>.ErrorResponse(
                "Failed to create association",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AssociationDto>> UpdateAsync(Guid id, UpdateAssociationRequest request)
    {
        try
        {
            var association = await _context.Associations.FindAsync(id);
            if (association == null)
            {
                return ApiResponse<AssociationDto>.ErrorResponse("Association not found");
            }

            var previousImageUrl = association.ImageUrl;

            if (request.Name != null) association.Name = request.Name;
            if (request.NameEn != null) association.NameEn = NormalizeOptional(request.NameEn);
            if (request.Description != null) association.Description = request.Description;
            if (request.DescriptionEn != null) association.DescriptionEn = NormalizeOptional(request.DescriptionEn);
            if (request.Province != null) association.Province = request.Province;
            if (request.City != null) association.City = request.City;
            if (request.Contact != null) association.Contact = request.Contact;
            if (request.Phone != null) association.Phone = request.Phone;
            if (request.President != null) association.President = request.President;
            if (request.MemberCount != null) association.MemberCount = request.MemberCount;
            if (request.FoundedYear.HasValue) association.FoundedYear = request.FoundedYear;
            if (request.ImageUrl != null) association.ImageUrl = NormalizeOptional(request.ImageUrl);
            if (request.Website != null) association.Website = request.Website;
            if (request.Domains != null) association.Domains = request.Domains;
            if (request.DomainsEn != null) association.DomainsEn = request.DomainsEn;
            if (request.OrganizationType != null) association.OrganizationType = NormalizeOrganizationType(request.OrganizationType);
            if (request.IsActive.HasValue) association.IsActive = request.IsActive.Value;

            association.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (request.ImageUrl != null
                && !string.Equals(previousImageUrl, association.ImageUrl, StringComparison.Ordinal)
                && IsOwnedUpload(previousImageUrl))
            {
                await _fileStorage.DeleteAsync(previousImageUrl);
            }

            return ApiResponse<AssociationDto>.SuccessResponse(MapToDto(association));
        }
        catch (Exception ex)
        {
            return ApiResponse<AssociationDto>.ErrorResponse(
                "Failed to update association",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try
        {
            var association = await _context.Associations.FindAsync(id);
            if (association == null)
            {
                return ApiResponse.CreateError("Association not found");
            }

            association.IsActive = false;
            association.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("Association deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete association",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<MediaUploadDto>> UploadImageAsync(Guid id, IFormFile file)
    {
        try
        {
            var association = await _context.Associations.FindAsync(id);
            if (association == null)
            {
                return ApiResponse<MediaUploadDto>.ErrorResponse("Association not found");
            }

            if (!_fileStorage.IsAllowedImageExtension(file.FileName))
            {
                return ApiResponse<MediaUploadDto>.ErrorResponse("Only image files are allowed");
            }

            var previousImageUrl = association.ImageUrl;
            var (relativeUrl, _) = await _fileStorage.SaveAsync(file, "associations");

            association.ImageUrl = relativeUrl;
            association.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (IsOwnedUpload(previousImageUrl)
                && !string.Equals(previousImageUrl, relativeUrl, StringComparison.Ordinal))
            {
                await _fileStorage.DeleteAsync(previousImageUrl);
            }

            return ApiResponse<MediaUploadDto>.SuccessResponse(new MediaUploadDto(
                relativeUrl,
                file.FileName,
                file.ContentType,
                file.Length));
        }
        catch (Exception ex)
        {
            return ApiResponse<MediaUploadDto>.ErrorResponse(
                "Failed to upload association image",
                new List<string> { ex.Message });
        }
    }

    private static AssociationDto MapToDto(Association association)
    {
        return new AssociationDto(
            association.Id,
            association.Name,
            association.Description,
            association.Province,
            association.City,
            association.Contact,
            association.Phone,
            association.President,
            association.MemberCount,
            association.FoundedYear,
            association.ImageUrl,
            association.Website,
            association.Domains,
            association.IsActive,
            association.CreatedAt,
            association.UpdatedAt,
            association.NameEn,
            association.DescriptionEn,
            association.DomainsEn,
            association.OrganizationType
        );
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeOrganizationType(string? value) =>
        string.Equals(value, "Committee", StringComparison.OrdinalIgnoreCase) ? "Committee" : "Association";

    private static bool IsOwnedUpload(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);
}
