using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.Models.Person;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class ParticipationsRepository : IParticipationsRepository
    {
        private readonly IParticipationsDataProvider _participationsDataProvider;
        private readonly IMapper _mapper;

        public ParticipationsRepository(IParticipationsDataProvider participationsDataProvider,
            IMapper mapper)
        {
            _participationsDataProvider = participationsDataProvider;
            _mapper = mapper;
        }

        public async Task LeaveEventAsync(Guid accountId, Guid eventId)
        {
            await _participationsDataProvider.LeaveEventAsync(accountId, eventId);
        }

        public async Task<Guid> ParticipateAsync(Guid accountId, Guid eventId)
        {
            return await _participationsDataProvider.ParticipateAsync(accountId, eventId);
        }

        public async Task<List<PersonInfo>> GetEventParticipantsAsync(Guid eventId)
        {
            var personItems = await _participationsDataProvider.GetEventParticipantsAsync(eventId);
            var result = _mapper.Map<List<PersonInfo>>(personItems);
            return result;
        }
    }
}
