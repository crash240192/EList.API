using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.EventTemplates;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class EventTemplatesRepository : IEventTemplatesRepository
    {
        private readonly IEventTemplatesDataProvider _eventTemplatesDataProvider;
        private readonly IMapper _mapper;

        public EventTemplatesRepository(IEventTemplatesDataProvider eventTemplatesDataProvider,
            IMapper mapper)
        {
            _eventTemplatesDataProvider = eventTemplatesDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateAsync(EventTemplate item)
        {
            var mappedItem = _mapper.Map<EventTemplateDto>(item);
            return await _eventTemplatesDataProvider.CreateAsync(mappedItem);
        }

        public async Task<EventTemplate?> GetByIdAsync(Guid id)
        {
            var item = await _eventTemplatesDataProvider.GetByIdAsync(id);
            return _mapper.Map<EventTemplate>(item);
        }

        public async Task UpdateAsync(EventTemplate item)
        {
            var mappedItem = _mapper.Map<EventTemplateDto>(item);
            await _eventTemplatesDataProvider.UpdateAsync(mappedItem);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _eventTemplatesDataProvider.DeleteAsync(id);
        }

        public async Task<List<EventTemplate>> SearchByAccountIdAsync(Guid accountId, string? name = null)
        {
            var items = await _eventTemplatesDataProvider.SearchByAccountIdAsync(accountId, name);
            return _mapper.Map<List<EventTemplate>>(items);
        }

        public async Task<List<EventTemplate>> SearchByOrganizationIdAsync(Guid organizationId, string? name = null)
        {
            var items = await _eventTemplatesDataProvider.SearchByOrganizationIdAsync(organizationId, name);
            return _mapper.Map<List<EventTemplate>>(items);
        }
    }
}
