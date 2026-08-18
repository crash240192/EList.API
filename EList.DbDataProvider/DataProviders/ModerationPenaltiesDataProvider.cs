using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class ModerationPenaltiesDataProvider : DataProviderBase, IModerationPenaltiesDataProvider
    {
        public ModerationPenaltiesDataProvider(IDataConnectionProvider dataConnectionProvider)
            : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateAsync(ModerationPenaltyDto item)
        {
            if (item.Id == Guid.Empty)
                item.Id = Guid.NewGuid();
            if (item.CreatedAt == default)
                item.CreatedAt = DateTimeOffset.UtcNow;
            if (item.StartsAt == default)
                item.StartsAt = DateTimeOffset.UtcNow;

            await _connection.InsertAsync(item);
            return item.Id;
        }

        public async Task<ModerationPenaltyDto?> GetByIdAsync(Guid id)
        {
            return await _connection.ModerationPenalties.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<ModerationPenaltyDto>> GetActiveByAccountAsync(Guid accountId)
        {
            var now = DateTimeOffset.UtcNow;
            return await ActiveQuery()
                .Where(i => i.AccountId == accountId && (i.EndsAt == null || i.EndsAt > now))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ModerationPenaltyDto>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            var now = DateTimeOffset.UtcNow;
            return await ActiveQuery()
                .Where(i => i.OrganizationId == organizationId && (i.EndsAt == null || i.EndsAt > now))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ModerationPenaltyDto>> GetActiveByEventAsync(Guid eventId)
        {
            var now = DateTimeOffset.UtcNow;
            return await ActiveQuery()
                .Where(i => i.EventId == eventId && (i.EndsAt == null || i.EndsAt > now))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<ModerationPenaltyDto?> FindActiveAsync(
            ModerationPenaltyType type,
            Guid? accountId,
            Guid? organizationId,
            Guid? eventId)
        {
            var now = DateTimeOffset.UtcNow;
            var query = ActiveQuery()
                .Where(i => i.PenaltyType == type && (i.EndsAt == null || i.EndsAt > now));

            if (accountId != null)
                query = query.Where(i => i.AccountId == accountId);
            if (organizationId != null)
                query = query.Where(i => i.OrganizationId == organizationId);
            if (eventId != null)
                query = query.Where(i => i.EventId == eventId);

            return await query.OrderByDescending(i => i.CreatedAt).FirstOrDefaultAsync();
        }

        public async Task<List<ModerationPenaltyDto>> GetExpiredUnliftedAsync(Guid? accountId = null, Guid? organizationId = null)
        {
            var now = DateTimeOffset.UtcNow;
            var query = _connection.ModerationPenalties
                .Where(i => i.RevokedAt == null && i.LiftedAt == null && i.EndsAt != null && i.EndsAt <= now);

            if (accountId != null)
                query = query.Where(i => i.AccountId == accountId);
            if (organizationId != null)
                query = query.Where(i => i.OrganizationId == organizationId);

            return await query.ToListAsync();
        }

        public async Task MarkRevokedAsync(Guid id, Guid revokedBy, DateTimeOffset at)
        {
            await _connection.ModerationPenalties.Where(i => i.Id == id)
                .Set(i => i.RevokedAt, at)
                .Set(i => i.RevokedBy, revokedBy)
                .Set(i => i.LiftedAt, at)
                .UpdateAsync();
        }

        public async Task MarkLiftedAsync(Guid id, DateTimeOffset at)
        {
            await _connection.ModerationPenalties.Where(i => i.Id == id)
                .Set(i => i.LiftedAt, at)
                .UpdateAsync();
        }

        private IQueryable<ModerationPenaltyDto> ActiveQuery()
        {
            return _connection.ModerationPenalties
                .Where(i => i.RevokedAt == null && i.LiftedAt == null);
        }
    }
}
