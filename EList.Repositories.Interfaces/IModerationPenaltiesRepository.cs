using EList.Models.ContentReports;
using EList.Models.Enums;

namespace EList.Repositories.Interfaces
{
    public interface IModerationPenaltiesRepository
    {
        Task<Guid> CreateAsync(ModerationPenalty penalty);
        Task<ModerationPenalty?> GetByIdAsync(Guid id);
        Task<List<ModerationPenalty>> GetActiveByAccountAsync(Guid accountId);
        Task<List<ModerationPenalty>> GetActiveByOrganizationAsync(Guid organizationId);
        Task<List<ModerationPenalty>> GetActiveByEventAsync(Guid eventId);
        Task<ModerationPenalty?> FindActiveAsync(
            ModerationPenaltyType type,
            Guid? accountId = null,
            Guid? organizationId = null,
            Guid? eventId = null);
        Task<List<ModerationPenalty>> GetExpiredUnliftedAsync(Guid? accountId = null, Guid? organizationId = null);
        Task MarkRevokedAsync(Guid id, Guid revokedBy, DateTimeOffset at);
        Task MarkLiftedAsync(Guid id, DateTimeOffset at);
    }
}
