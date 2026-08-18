using EList.Common.Models;
using EList.Models.ContentReports;
using EList.Models.Enums;

namespace EList.Services.Interfaces
{
    public interface IModerationPenaltiesService
    {
        Task LiftExpiredForAccountAsync(Guid accountId);
        Task LiftExpiredForOrganizationAsync(Guid organizationId);

        Task<CommandResult> AssertNotRestrictedAsync(
            Guid accountId,
            ModerationPenaltyType type,
            Guid? eventId = null);

        Task<List<ModerationPenalty>> GetActiveForAccountAsync(Guid accountId);
        Task<List<ModerationPenalty>> GetActiveForOrganizationAsync(Guid organizationId);
        Task<List<ModerationPenalty>> GetActiveForEventAsync(Guid eventId);

        Task<CommandResult<Guid>> ApplyAsync(ModerationPenalty penalty);
        Task<CommandResult> RevokeAsync(Guid penaltyId, string? comment);
    }
}
