using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class MemberExperienceService(ApplicationDbContext context) : IMemberExperienceService
{
    public async Task<ApiResponse<MemberOnboardingDto>> GetOnboardingAsync(Guid userId)
    {
        var user = await context.Users.AsNoTracking().Include(item => item.Member)
            .SingleOrDefaultAsync(item => item.Id == userId && item.IsActive);
        if (user?.Member is null) return ApiResponse<MemberOnboardingDto>.ErrorResponse("Member account not found");
        var preferences = await GetOrCreateAsync(userId);
        var hasCommunityProfile = await context.NetworkingProfiles.AsNoTracking().AnyAsync(item => item.MemberId == user.MemberId);
        var hasEventRegistration = await context.EventRegistrations.AsNoTracking().AnyAsync(item => item.MemberId == user.MemberId && item.Status != "Cancelled");
        var member = user.Member;
        var profileComplete = !string.IsNullOrWhiteSpace(member.FirstName) && !string.IsNullOrWhiteSpace(member.LastName)
            && !string.IsNullOrWhiteSpace(member.Phone) && !string.IsNullOrWhiteSpace(member.City) && !string.IsNullOrWhiteSpace(member.Province);
        var steps = new List<OnboardingStepDto>
        {
            new("member-profile", "Compléter mes coordonnées", profileComplete, "/espace-membre?section=profile"),
            new("preferences", "Choisir mes communications", preferences.HasCompletedPreferences, "/espace-membre?section=preferences"),
            new("community-profile", "Créer ma carte communautaire", hasCommunityProfile, "/espace-membre?section=profile"),
            new("first-event", "Découvrir un événement", hasEventRegistration, "/actualites/evenements")
        };
        var percent = (int)Math.Round(steps.Count(item => item.Completed) * 100d / steps.Count);
        return ApiResponse<MemberOnboardingDto>.SuccessResponse(new(percent, percent == 100, steps, Map(preferences)));
    }

    public async Task<ApiResponse<MemberPreferenceDto>> UpdatePreferencesAsync(Guid userId, UpdateMemberPreferenceRequest request)
    {
        if (request.PreferredLanguage is not ("fr" or "en")) return ApiResponse<MemberPreferenceDto>.ErrorResponse("Preferred language must be fr or en");
        if (string.IsNullOrWhiteSpace(request.TimeZone) || request.TimeZone.Length > 100) return ApiResponse<MemberPreferenceDto>.ErrorResponse("A valid time zone is required");
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId && item.IsActive && item.MemberId != null);
        if (user is null) return ApiResponse<MemberPreferenceDto>.ErrorResponse("Member account not found");
        var item = await GetOrCreateAsync(userId);
        item.PreferredLanguage = request.PreferredLanguage;
        item.TimeZone = request.TimeZone.Trim();
        item.EmailEvents = request.EmailEvents;
        item.EmailOpportunities = request.EmailOpportunities;
        item.EmailMentorship = request.EmailMentorship;
        item.EmailServiceUpdates = request.EmailServiceUpdates;
        item.EmailNewsletter = request.EmailNewsletter;
        item.PushNotifications = request.PushNotifications;
        item.HasCompletedPreferences = true;
        item.UpdatedAt = DateTime.UtcNow;

        // A preference-centre opt-out must also override a newsletter subscription
        // created separately on the public website. Re-enabling remains an explicit
        // choice here; it never silently recreates a deleted subscription record.
        if (!request.EmailNewsletter)
        {
            var subscriptions = await context.NewsletterSubscriptions
                .Where(subscription => subscription.Email == user.Email && subscription.IsActive)
                .ToListAsync();
            foreach (var subscription in subscriptions)
            {
                subscription.IsActive = false;
                subscription.UpdatedAt = DateTime.UtcNow;
            }
        }
        await context.SaveChangesAsync();
        return ApiResponse<MemberPreferenceDto>.SuccessResponse(Map(item));
    }

    private async Task<MemberPreference> GetOrCreateAsync(Guid userId)
    {
        var item = await context.MemberPreferences.FirstOrDefaultAsync(candidate => candidate.UserId == userId);
        if (item is not null) return item;
        item = new MemberPreference { UserId = userId };
        context.MemberPreferences.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private static MemberPreferenceDto Map(MemberPreference item) => new(
        item.PreferredLanguage, item.TimeZone, item.EmailEvents, item.EmailOpportunities,
        item.EmailMentorship, item.EmailServiceUpdates, item.EmailNewsletter,
        item.PushNotifications, item.HasCompletedPreferences, item.UpdatedAt);
}
