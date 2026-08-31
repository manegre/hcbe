using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public ProjectService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<List<ProjectDto>>> GetAllAsync()
    {
        try
        {
            var projects = await _context.Projects
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var projectDtos = projects.Select(MapToDto).ToList();
            return ApiResponse<List<ProjectDto>>.SuccessResponse(projectDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ProjectDto>>.ErrorResponse(
                "Failed to retrieve projects",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<ProjectDto>>> GetAllForAdminAsync()
    {
        try
        {
            var projects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var projectDtos = projects.Select(MapToDto).ToList();
            return ApiResponse<List<ProjectDto>>.SuccessResponse(projectDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ProjectDto>>.ErrorResponse(
                "Failed to retrieve projects",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ProjectDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var project = await _context.Projects
                .Where(p => p.IsActive)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (project == null)
            {
                return ApiResponse<ProjectDto>.ErrorResponse("Project not found");
            }

            return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(project));
        }
        catch (Exception ex)
        {
            return ApiResponse<ProjectDto>.ErrorResponse(
                "Failed to retrieve project",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ProjectDto>> GetByIdForAdminAsync(Guid id)
    {
        try
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return ApiResponse<ProjectDto>.ErrorResponse("Project not found");
            }

            return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(project));
        }
        catch (Exception ex)
        {
            return ApiResponse<ProjectDto>.ErrorResponse(
                "Failed to retrieve project",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ProjectDto>> CreateAsync(CreateProjectRequest request)
    {
        try
        {
            var project = new Project
            {
                Title = request.Title,
                TitleEn = NormalizeOptional(request.TitleEn),
                Location = request.Location,
                LocationEn = NormalizeOptional(request.LocationEn),
                Type = request.Type,
                Status = request.Status,
                Progress = request.Progress,
                Description = request.Description,
                DescriptionEn = NormalizeOptional(request.DescriptionEn),
                ImageUrl = request.ImageUrl,
                Budget = request.Budget,
                FundsRaised = request.FundsRaised,
                Beneficiaries = request.Beneficiaries,
                BeneficiariesEn = NormalizeOptional(request.BeneficiariesEn),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Partners = request.Partners ?? new List<string>()
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Create notification
            await _notificationService.CreateNotificationAsync(
                "project",
                "Nouveau projet créé",
                project.Title,
                project.Id,
                "#projects"
            );

            return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(project));
        }
        catch (Exception ex)
        {
            return ApiResponse<ProjectDto>.ErrorResponse(
                "Failed to create project",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ProjectDto>> UpdateAsync(Guid id, UpdateProjectRequest request)
    {
        try
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return ApiResponse<ProjectDto>.ErrorResponse("Project not found");
            }

            if (!string.IsNullOrEmpty(request.Title)) project.Title = request.Title;
            if (request.TitleEn != null) project.TitleEn = NormalizeOptional(request.TitleEn);
            if (!string.IsNullOrEmpty(request.Location)) project.Location = request.Location;
            if (request.LocationEn != null) project.LocationEn = NormalizeOptional(request.LocationEn);
            if (!string.IsNullOrEmpty(request.Type)) project.Type = request.Type;
            if (!string.IsNullOrEmpty(request.Status)) project.Status = request.Status;
            if (request.Progress.HasValue) project.Progress = request.Progress.Value;
            if (!string.IsNullOrEmpty(request.Description)) project.Description = request.Description;
            if (request.DescriptionEn != null) project.DescriptionEn = NormalizeOptional(request.DescriptionEn);
            if (request.ImageUrl != null) project.ImageUrl = request.ImageUrl;
            if (!string.IsNullOrEmpty(request.Budget)) project.Budget = request.Budget;
            if (!string.IsNullOrEmpty(request.FundsRaised)) project.FundsRaised = request.FundsRaised;
            if (!string.IsNullOrEmpty(request.Beneficiaries)) project.Beneficiaries = request.Beneficiaries;
            if (request.BeneficiariesEn != null) project.BeneficiariesEn = NormalizeOptional(request.BeneficiariesEn);
            if (request.StartDate.HasValue) project.StartDate = request.StartDate;
            if (request.EndDate.HasValue) project.EndDate = request.EndDate;
            if (request.Partners != null) 
                project.Partners = request.Partners;
            if (request.IsActive.HasValue) project.IsActive = request.IsActive.Value;
            
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(project));
        }
        catch (Exception ex)
        {
            return ApiResponse<ProjectDto>.ErrorResponse(
                "Failed to update project",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ProjectDto>> UpdateProgressAsync(Guid id, int progress)
    {
        try
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return ApiResponse<ProjectDto>.ErrorResponse("Project not found");
            }

            if (progress < 0 || progress > 100)
            {
                return ApiResponse<ProjectDto>.ErrorResponse(
                    "Progress must be between 0 and 100",
                    new List<string> { "Invalid progress value" });
            }

            project.Progress = progress;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(project));
        }
        catch (Exception ex)
        {
            return ApiResponse<ProjectDto>.ErrorResponse(
                "Failed to update project progress",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return ApiResponse.CreateError("Project not found");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("Project deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete project",
                new List<string> { ex.Message });
        }
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto(
            project.Id,
            project.Title,
            project.Location,
            project.Type,
            project.Status,
            project.Progress,
            project.Description,
            project.ImageUrl,
            project.Budget,
            project.FundsRaised,
            project.Beneficiaries,
            project.StartDate,
            project.EndDate,
            project.Partners ?? new List<string>(),
            project.IsActive,
            project.CreatedAt,
            project.UpdatedAt,
            project.TitleEn,
            project.DescriptionEn,
            project.LocationEn,
            project.BeneficiariesEn
        );
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

