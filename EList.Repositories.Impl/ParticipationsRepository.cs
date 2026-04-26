using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Participation;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using NetTopologySuite.Index.HPRtree;

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

        public async Task<PagedList<Participant>> GetEventParticipantsAsync(EventParticipantsSearchRequest request)
        {
            var mappedRequest = _mapper.Map<DbDataProvider.Models.SearchRequests.EventParticipantsSearchRequest>(request);
            var participantsResult = await _participationsDataProvider.GetEventParticipantsAsync(mappedRequest);

            var resultList = participantsResult.Item2.Select(i => new Participant
            {
                Account = _mapper.Map<Account>(i),
                PersonInfo = _mapper.Map<PersonInfo>(i.PersonInfo)
            }).ToList();

            return new PagedList<Participant>(participantsResult.Item1, resultList, request.PageIndex ?? 1, request.PageSize ?? participantsResult.Item1);
        }
    }
}
