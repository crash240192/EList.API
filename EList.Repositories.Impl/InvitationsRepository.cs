using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
using EList.Models.Invitations;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using NetTopologySuite.Index.HPRtree;

namespace EList.Repositories.Impl
{
    public class InvitationsRepository : IInvitationsRepository
    {
        private readonly IMapper _mapper;
        private readonly IInvitationsDataProvider _invitationsDataProvider;
        public InvitationsRepository(IInvitationsDataProvider invitationsDataProvider,
            IMapper mapper)
        {
            _invitationsDataProvider = invitationsDataProvider;
            _mapper = mapper;
        }

        public async Task CreateInvitationsAsync(CreateInvitationsRequest request, Guid inviterAccountId)
        {
            var invitationItems = request.AccountIds?.Select(i => new InvitationDto
            {
                CreationDate = DateTimeOffset.Now,
                EventId = request.EventId,
                InvitedAccountId = i,
                InviterOrganizationId = request.InviterOrganizationId,
                InviterAccountId = inviterAccountId
            })?.ToList();

            await _invitationsDataProvider.CreateInvitationsAsync(invitationItems);
        }

        public async Task DeleteInvitationAsync(Guid id)
        {
            await _invitationsDataProvider.DeleteInvitationAsync(id);
        }

        public async Task CancelAllInvitationsAsync(Guid eventId)
        {
            await _invitationsDataProvider.CancelInvitationsAsync(eventId);
        }

        public async Task DeleteInvitationAsync(Guid eventId, Guid accountId)
        {
            await _invitationsDataProvider.DeleteInvitationAsync(eventId, accountId);
        }

        public async Task<PagedList<Invitation>> SearchInvitationsAsync(InvitationsSearchRequest request)
        {
            var mappedRequest = new DbDataProvider.Models.SearchRequests.InvitationsSearchRequest
            {
                EventIds = request.EventIds,
                InvitedAccountIds = request.InvitedAccountIds,
                InviterAccountIds = request.InviterAccountIds,
                InviterOrgIds = request.InviterOrgIds,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
            var items = await _invitationsDataProvider.SearchInvitationsAsync(mappedRequest);

            var resultList = items.Items?.Select(i =>
            {
                var mappedItem = _mapper.Map<Invitation>(i);
                mappedItem.Event = _mapper.Map<Event>(i.Event);
                mappedItem.Event.Types = i.Event.Types.Select(i => i.Type).Select(i => _mapper.Map<EventType>(i)).ToList();
                mappedItem.Inviter = new Inviter
                {
                    Account = _mapper.Map<AccountPublicData>(i.Inviter),
                    PersonInfo = _mapper.Map<PersonInfo>(i.Inviter.PersonInfo)
                };
                return mappedItem;
            })?.ToList();

            return new PagedList<Invitation>(items.TotalCount, resultList, request.PageIndex ?? 1, request.PageSize ?? items.TotalCount);
        }

        public async Task<Invitation> GetInvitationAsync(Guid invitationId)
        {
            var invitation = await _invitationsDataProvider.GetInvitationAsync(invitationId);
            var result = _mapper.Map<Invitation>(invitation);
            return result;
        }

        public async Task<Invitation> GetInvitationAsync(Guid invitedAccountId, Guid eventId)
        {
            var invitation = await _invitationsDataProvider.GetInvitationAsync(invitedAccountId, eventId);
            var result = _mapper.Map<Invitation>(invitation);
            return result;
        }

        public async Task<bool> IsUserInvitatedAsync(Guid accountId, Guid eventId)
        {
            var result = await _invitationsDataProvider.IsUserInvitatedAsync(accountId, eventId);
            return result;
        }
    }
}
