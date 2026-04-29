using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class InvitationsDataProvider : DataProviderBase, IInvitationsDataProvider
    {
        public InvitationsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task CreateInvitationsAsync(InvitationDto invitation)
        {
            if (!_connection.Invitations.Any(i => i.EventId == invitation.Id && i.InvitedAccountId == invitation.InvitedAccountId))
            {
                invitation.CreationDate = DateTime.Now;
                await _connection.InsertWithIdentityAsync(invitation);
            }
        }

        public async Task CreateInvitationsAsync(List<InvitationDto> invitations)
        {
            if (invitations?.Any() ?? false)
            {
                var existingInvitations = await _connection.Invitations.Where(i => invitations.Any(inv => inv.EventId == i.EventId && inv.InvitedAccountId == i.InvitedAccountId)).ToListAsync();
                invitations = invitations.Where(i => !existingInvitations.Any(inv => inv.InvitedAccountId == i.InvitedAccountId && inv.EventId == i.EventId)).ToList();
                invitations.ForEach(i => i.CreationDate = DateTime.Now);
                await _connection.BulkCopyAsync(invitations);
            }
        }

        public async Task<InvitationDto> GetInvitationAsync(Guid id)
        {
            var result = await _connection.Invitations.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<InvitationDto> GetInvitationAsync(Guid invitedAccountId, Guid eventId)
        {
            var result = await _connection.Invitations.FirstOrDefaultAsync(i => i.InvitedAccountId == invitedAccountId && i.EventId == eventId);
            return result;
        }

        public async Task DeleteInvitationAsync(Guid id)
        {
            await _connection.Invitations.DeleteAsync(i => i.Id == id);
        }

        public async Task DeleteInvitationAsync(Guid eventId, Guid accountId)
        {
            await _connection.Invitations.DeleteAsync(i => i.EventId == eventId && i.InvitedAccountId == accountId);
        }

        public async Task<(int, List<InvitationDto>)> SearchInvitationsAsync(InvitationsSearchRequest request)
        {
            var invitationsRequest = _connection.Invitations
                .LoadWith(i => i.Event)
                .ThenLoad(i => i.Parameters)
                .LoadWith(i => i.Event)
                .ThenLoad(i => i.Types)
                .ThenLoad(i => i.Type)
                .ThenLoad(i => i.EventCategory)
                .LoadWith(i => i.Inviter)
                .ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.Invited)
                .ThenLoad(i => i.PersonInfo)
                .OrderBy(i => i.CreationDate)
                .AsQueryable();

            if (request.InviterOrgIds?.Any() ?? false)
                invitationsRequest = invitationsRequest.Where(i => i.InviterOrganizationId != null)
                    .Where(i => request.InviterOrgIds.Contains(i.InviterOrganizationId.Value));

            if (request.InviterAccountIds?.Any() ?? false)
                invitationsRequest = invitationsRequest.Where(i => request.InviterAccountIds.Contains(i.InviterAccountId));

            if (request.InvitedAccountIds?.Any() ?? false)
                invitationsRequest = invitationsRequest.Where(i => request.InvitedAccountIds.Contains(i.InvitedAccountId));

            if (request.EventIds?.Any() ?? false)
                invitationsRequest = invitationsRequest.Where(i => request.EventIds.Contains(i.EventId));

            var totalCount = invitationsRequest.Count();

            List<InvitationDto> resultList;
            if (request.PageSize != null && request.PageIndex != null)
                resultList = await invitationsRequest.Skip(request.PageSize.Value * (request.PageIndex.Value)).Take(request.PageSize.Value).ToListAsync();
            else
                resultList = await invitationsRequest.ToListAsync();

            return (totalCount, resultList);
        }
    }
}
