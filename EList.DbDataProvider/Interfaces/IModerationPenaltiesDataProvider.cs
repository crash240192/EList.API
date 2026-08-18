using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IModerationPenaltiesDataProvider
    {
        Task<Guid> CreateAsync(ModerationPenaltyDto item);
        Task<ModerationPenaltyDto?> GetByIdAsync(Guid id);
        Task<List<ModerationPenaltyDto>> GetActiveByAccountAsync(Guid accountId);
        Task<List<ModerationPenaltyDto>> GetActiveByOrganizationAsync(Guid organizationId);
        Task<List<ModerationPenaltyDto>> GetActiveByEventAsync(Guid eventId);
        Task<ModerationPenaltyDto?> FindActiveAsync(
            ModerationPenaltyType type,
            Guid? accountId,
            Guid? organizationId,
            Guid? eventId);
        Task<List<ModerationPenaltyDto>> GetExpiredUnliftedAsync(Guid? accountId = null, Guid? organizationId = null);
        Task MarkRevokedAsync(Guid id, Guid revokedBy, DateTimeOffset at);
        Task MarkLiftedAsync(Guid id, DateTimeOffset at);
    }
}
