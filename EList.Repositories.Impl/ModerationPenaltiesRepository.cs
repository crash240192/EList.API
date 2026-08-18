using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.ContentReports;
using EList.Models.Enums;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class ModerationPenaltiesRepository : IModerationPenaltiesRepository
    {
        private readonly IModerationPenaltiesDataProvider _dataProvider;
        private readonly IMapper _mapper;

        public ModerationPenaltiesRepository(IModerationPenaltiesDataProvider dataProvider, IMapper mapper)
        {
            _dataProvider = dataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateAsync(ModerationPenalty penalty)
        {
            var mapped = _mapper.Map<ModerationPenaltyDto>(penalty);
            return await _dataProvider.CreateAsync(mapped);
        }

        public async Task<ModerationPenalty?> GetByIdAsync(Guid id)
        {
            var item = await _dataProvider.GetByIdAsync(id);
            return _mapper.Map<ModerationPenalty>(item);
        }

        public async Task<List<ModerationPenalty>> GetActiveByAccountAsync(Guid accountId)
        {
            var items = await _dataProvider.GetActiveByAccountAsync(accountId);
            return _mapper.Map<List<ModerationPenalty>>(items);
        }

        public async Task<List<ModerationPenalty>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            var items = await _dataProvider.GetActiveByOrganizationAsync(organizationId);
            return _mapper.Map<List<ModerationPenalty>>(items);
        }

        public async Task<List<ModerationPenalty>> GetActiveByEventAsync(Guid eventId)
        {
            var items = await _dataProvider.GetActiveByEventAsync(eventId);
            return _mapper.Map<List<ModerationPenalty>>(items);
        }

        public async Task<ModerationPenalty?> FindActiveAsync(
            ModerationPenaltyType type,
            Guid? accountId = null,
            Guid? organizationId = null,
            Guid? eventId = null)
        {
            var dbType = _mapper.Map<DbDataProvider.Models.Enums.ModerationPenaltyType>(type);
            var item = await _dataProvider.FindActiveAsync(dbType, accountId, organizationId, eventId);
            return _mapper.Map<ModerationPenalty>(item);
        }

        public async Task<List<ModerationPenalty>> GetExpiredUnliftedAsync(Guid? accountId = null, Guid? organizationId = null)
        {
            var items = await _dataProvider.GetExpiredUnliftedAsync(accountId, organizationId);
            return _mapper.Map<List<ModerationPenalty>>(items);
        }

        public async Task MarkRevokedAsync(Guid id, Guid revokedBy, DateTimeOffset at)
        {
            await _dataProvider.MarkRevokedAsync(id, revokedBy, at);
        }

        public async Task MarkLiftedAsync(Guid id, DateTimeOffset at)
        {
            await _dataProvider.MarkLiftedAsync(id, at);
        }
    }
}
