using HcbeApi.Helpers;
using HcbeApi.Models;
namespace HcbeApi.Services;
public interface IImpactAnalyticsService { Task<ApiResponse<ImpactDashboardDto>> GetAsync(); }
