using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.EventOrganizators;
using EList.Models.Person;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class EventOrganizatorsRepository : IEventOrganizatorsRepository
    {
        private readonly IEventOrganizatorsDataProvider _eventOrganizatorsDataProvider;
        private readonly IMapper _mapper;

        public EventOrganizatorsRepository(IEventOrganizatorsDataProvider eventOrganizatorsDataProvider,
            IMapper mapper)
        {
            _eventOrganizatorsDataProvider = eventOrganizatorsDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateAsync(EventOrganizatorRequest request)
        {
            var mappedRequest = new EventOrganizatorDto
            {
                AccountId = request.AccountId,
                OrganizationId = request.OrganizationId,
                EventId = request.EventId,
            };
            var result = await _eventOrganizatorsDataProvider.CreateAsync(mappedRequest);
            return result;
        }

        public async Task UpdateAsync(Guid id, EventOrganizatorRequest request)
        {
            var mappedRequest = new EventOrganizatorDto
            {
                AccountId = request.AccountId,
                OrganizationId = request.OrganizationId,
                EventId = request.EventId,
                Id = id
            };
            await _eventOrganizatorsDataProvider.UpdateAsync(mappedRequest);
        }

        public async Task<List<EventOrganizator>?> GetByEventIdAsync(Guid eventId)
        {
            var items = await _eventOrganizatorsDataProvider.GetByEventIdAsync(eventId);
            var result = items?.Select(i => new EventOrganizator
            {
                Account = _mapper.Map<AccountPublicData>(i.Account),
                PersonInfo = _mapper.Map<PersonInfo>(i.Account.PersonInfo),
                Id = i.Id,
                EventId = eventId,
                OrganizationId = i.OrganizationId
            }).ToList();
            return result;
        }

        public async Task<List<Guid>> GetOrganizatorIdsByEventIdAsync(Guid eventId)
        {
            var organizators = await _eventOrganizatorsDataProvider.GetOrganizatorIdsByEventIdAsync(eventId);
            return organizators;
        }

        public async Task<EventOrganizator?> GetByIdAsync(Guid id)
        {
            var item = await _eventOrganizatorsDataProvider.GetByIdAsync(id);
            var result = _mapper.Map<EventOrganizator>(item);
            return result;
        }

        public async Task AssignAsync(Guid eventId, List<Guid> accountIds)
        {
            await _eventOrganizatorsDataProvider.AssignAsync(eventId, accountIds);
        }
    }
}
