using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Invitations;
using EList.Repositories.Interfaces;

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
            var invitationItems = request.AccountIds.Select(i => new InvitationDto
            {
                CreationDate = DateTimeOffset.Now,
                EventId = request.EventId,
                InvitedAccountId = i,
                InviterOrganizationId = request.InviterOrganizationId,
                InviterAccountId = inviterAccountId
            });

            foreach (var invite in invitationItems)
                await _invitationsDataProvider.CreateInvitationsAsync(invite);
        }

        public async Task DeleteInvitationAsync(Guid id)
        {
            await _invitationsDataProvider.DeleteInvitationAsync(id);
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
            var resultList = _mapper.Map<List<Invitation>>(items.Item2);

            return new PagedList<Invitation>(items.Item1, resultList, request.PageIndex ?? 1, request.PageSize ?? items.Item1);
        }

        public async Task<Invitation> GetInvitationAsync(Guid invitationId)
        {
            var invitation = await _invitationsDataProvider.GetInvitationAsync(invitationId);
            var result = _mapper.Map<Invitation>(invitation);
            return result;
        }
    }
}
