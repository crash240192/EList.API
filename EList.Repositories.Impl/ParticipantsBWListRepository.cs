using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Participation;
using EList.Models.Person;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class ParticipantsBWListRepository : IParticipantsBWListRepository
    {
        private readonly IParticipantsBWListDataProvider _participantsBWListDataProvider;
        private readonly IMapper _mapper;

        public ParticipantsBWListRepository(IParticipantsBWListDataProvider participantsBWListDataProvider,
            IMapper mapper) 
        {
            _participantsBWListDataProvider = participantsBWListDataProvider;
            _mapper = mapper;
        }


        public async Task AddToBlackListAsync(AddUsersToBWListRequest request)
        {
            await _participantsBWListDataProvider.AddToBlackListAsync(request.EventId, request.AccountIds);
        }

        public async Task AddToWhiteListAsync(AddUsersToBWListRequest request)
        {
            await _participantsBWListDataProvider.AddToWhiteListAsync(request.EventId, request.AccountIds);
        }


        public async Task DeleteFromBlackListAsync(Guid eventId, Guid accountId)
        {
            await _participantsBWListDataProvider.DeleteFromBlackListAsync(eventId, accountId);
        }

        public async Task DeleteFromWhiteListAsync(Guid eventId, Guid accountId)
        {
            await _participantsBWListDataProvider.DeleteFromWhiteListAsync(eventId, accountId);
        }


        public async Task<PagedList<ParticipantBlackListItem>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var blackList = await _participantsBWListDataProvider.GetEventBlackListAsync(eventId, pageIndex, pageSize);
            var resultList = blackList.Items?.Select(i =>
            {
                var result = _mapper.Map<ParticipantBlackListItem>(i);
                result.PersonInfo = _mapper.Map<PersonInfo>(i.Account?.PersonInfo);
                return result;
            }).ToList();
            return new PagedList<ParticipantBlackListItem>(blackList.TotalCount, resultList, pageIndex ?? 0, pageSize ?? blackList.TotalCount);
        }

        public async Task<PagedList<ParticipantWhiteListItem>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var whiteList = await _participantsBWListDataProvider.GetEventWhiteListAsync(eventId, pageIndex, pageSize);
            var resultList = whiteList.Items?.Select(i =>
            {
                var result = _mapper.Map<ParticipantWhiteListItem>(i);
                result.PersonInfo = _mapper.Map<PersonInfo>(i.Account?.PersonInfo);
                return result;
            }).ToList();
            return new PagedList<ParticipantWhiteListItem>(whiteList.TotalCount, resultList, pageIndex ?? 0, pageSize ?? whiteList.TotalCount);
        }

        public async Task<List<Guid>> GetEventBlackListShortAsync(Guid eventId)
        {
            var result = await _participantsBWListDataProvider.GetEventBlackListShortAsync(eventId);
            return result;
        }

        public async Task<List<Guid>> GetEventWhiteListShortAsync(Guid eventId)
        {
            var result = await _participantsBWListDataProvider.GetEventWhiteListShortAsync(eventId);
            return result;
        }


        public async Task<int> WhiteListPersonsCountAsync(Guid eventId)
        {
            var result = await _participantsBWListDataProvider.WhiteListPersonsCountAsync(eventId);
            return result;
        }

        public async Task<int> BlackListPersonsCountAsync(Guid eventId)
        {
            var result = await _participantsBWListDataProvider.BlackListPersonsCountAsync(eventId);
            return result;
        }

        public async Task<bool> IsUserInBlackListAsync(Guid eventId, Guid accountId)
        {
            var result = await _participantsBWListDataProvider.IsUserInBlackListAsync(eventId,accountId);
            return result;
        }

        public async Task<bool> IsUserInWhiteListAsync(Guid eventId, Guid accountId)
        {
            var result = await _participantsBWListDataProvider.IsUserInWhiteListAsync(eventId, accountId);
            return result;
        }

        public async Task<bool> IsWhiteListEmptyAsync(Guid eventId)
        {   
            var result = await _participantsBWListDataProvider.IsWhiteListEmptyAsync(eventId);
            return result;
        }

        public async Task<List<Guid>> FilterUsersNotInWhiteListAsync(Guid eventId, List<Guid> accountIds)
        {
            var result = await _participantsBWListDataProvider.FilterUsersNotInWhiteListAsync(eventId, accountIds);
            return result;
        }

        public async Task<List<Guid>> FilterUsersNotInBlackListAsync(Guid eventId, List<Guid> accountIds)
        {
            var result = await _participantsBWListDataProvider.FilterUsersNotInBlackListAsync(eventId, accountIds);
            return result;
        }
    }
}
