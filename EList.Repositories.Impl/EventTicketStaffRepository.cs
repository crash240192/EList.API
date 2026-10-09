using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Events;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class EventTicketStaffRepository : IEventTicketStaffRepository
    {
        private readonly IEventTicketStaffDataProvider _dataProvider;
        private readonly IMapper _mapper;

        public EventTicketStaffRepository(IEventTicketStaffDataProvider dataProvider, IMapper mapper)
        {
            _dataProvider = dataProvider;
            _mapper = mapper;
        }

        public async Task<List<EventTicketStaff>> GetByEventIdAsync(Guid eventId)
        {
            var items = await _dataProvider.GetByEventIdAsync(eventId);
            return _mapper.Map<List<EventTicketStaff>>(items);
        }

        public async Task<EventTicketStaff?> GetAsync(Guid eventId, Guid accountId)
        {
            var item = await _dataProvider.GetAsync(eventId, accountId);
            return item == null ? null : _mapper.Map<EventTicketStaff>(item);
        }

        public async Task<bool> CanAccountCheckInAsync(Guid eventId, Guid accountId)
        {
            return await _dataProvider.CanAccountCheckInAsync(eventId, accountId);
        }

        public async Task<bool> CanAccountViewStatsAsync(Guid eventId, Guid accountId)
        {
            return await _dataProvider.CanAccountViewStatsAsync(eventId, accountId);
        }

        public async Task ReplaceForEventAsync(Guid eventId, List<EventTicketStaff> staff)
        {
            var mapped = _mapper.Map<List<EventTicketStaffDto>>(staff ?? new List<EventTicketStaff>());
            await _dataProvider.ReplaceForEventAsync(eventId, mapped);
        }

        public async Task DeleteByEventAndAccountAsync(Guid eventId, Guid accountId)
        {
            await _dataProvider.DeleteByEventAndAccountAsync(eventId, accountId);
        }
    }
}
