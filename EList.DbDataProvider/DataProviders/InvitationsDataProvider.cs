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
            if (!await _connection.Invitations.AnyAsync(i => i.EventId == invitation.Id && i.InvitedAccountId == invitation.InvitedAccountId))
            {
                invitation.CreationDate = DateTime.Now;
                await _connection.InsertWithIdentityAsync(invitation);
            }
        }

        public async Task CreateInvitationsAsync(List<InvitationDto> invitations)
        {
            if (invitations?.Any() ?? false)
            {
                var existingInvitationsQuery = _connection.Invitations.Where(i => invitations.Any(inv => inv.EventId == i.EventId && inv.InvitedAccountId == i.InvitedAccountId));
                var existingInvitations = await existingInvitationsQuery.ToListAsync();
                invitations = invitations.Where(i => !existingInvitations.Any(inv => inv.InvitedAccountId == i.InvitedAccountId && inv.EventId == i.EventId)).ToList();
                invitations.ForEach(i => i.CreationDate = DateTime.Now);
                await _connection.BulkCopyAsync(invitations);

                await existingInvitationsQuery
                    .Set(i => i.CreationDate, DateTime.Now)
                    .Set(i => i.Viewed, false)
                    .UpdateAsync();
            }
        }

        public async Task<InvitationDto> GetInvitationAsync(Guid id)
        {
            var result = await _connection.Invitations.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<InvitationDto> GetFullInvitationAsync(Guid id)
        {
            var result = await _connection.Invitations
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
                .FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<int> GetNotViewedInvitationsCountAsync(Guid accountId)
        {
            var result = await _connection.Invitations.Where(i => i.InvitedAccountId == accountId && i.Viewed == false).CountAsync();
            return result;
        }

        public async Task ViewInvitationAsync(Guid invitationId)
        {
            await _connection.Invitations.Where(i => i.Id == invitationId)
                .Set(i => i.Viewed, true)
                .UpdateAsync();
        }

        public async Task ViewAllInvitationsAsync(Guid accountId)
        {
            await _connection.Invitations.Where(i => i.InvitedAccountId == accountId)
                .Set(i => i.Viewed, true)
                .UpdateAsync();
        }

        public async Task<List<InvitationDto>?> GetAllEventInvitationsAsync(Guid eventId)
        {
            var result = await _connection.Invitations.Where(i => i.EventId == eventId).ToListAsync();
            return result;
        }

        public async Task<List<Guid>?> GetInvitedUsersAsync(Guid eventId)
        {
            var result = await _connection.Invitations.Where(i => i.EventId == eventId)
                .Select(i => i.InvitedAccountId)
                .ToListAsync();
            return result;
        }

        public async Task<InvitationDto> GetInvitationAsync(Guid invitedAccountId, Guid eventId)
        {
            var result = await _connection.Invitations.FirstOrDefaultAsync(i => i.InvitedAccountId == invitedAccountId && i.EventId == eventId);
            return result;
        }

        public async Task<bool> IsUserInvitatedAsync(Guid accountId, Guid eventId)
        {
            var result = await _connection.Invitations.AnyAsync(i => i.InvitedAccountId == accountId && i.EventId == eventId);
            return result;
        }

        public async Task DeleteInvitationAsync(Guid id)
        {
            await _connection.Invitations.DeleteAsync(i => i.Id == id);
        }

        public async Task CancelInvitationsAsync(Guid eventId)
        {
            await _connection.Invitations.DeleteAsync(i => i.EventId == eventId);
        }

        public async Task CancelAllInvitationsExceptThisUsersAsync(Guid eventId, List<Guid> invitedAccountIds)
        {
            await _connection.Invitations.DeleteAsync(i => i.EventId == eventId && !invitedAccountIds.Contains(i.InvitedAccountId));
        }

        public async Task CancelAllInvitationsExceptWhiteListAsync(Guid eventId)
        {
            var whiteList = await _connection.WhiteList.Where(i => i.EventId == eventId).Select(i => i.AccountId).ToListAsync();
            await _connection.Invitations.DeleteAsync(i => i.EventId == eventId && !whiteList.Contains(i.InvitedAccountId));
        }

        public async Task DeleteInvitationAsync(Guid eventId, Guid accountId)
        {
            await _connection.Invitations.DeleteAsync(i => i.EventId == eventId && i.InvitedAccountId == accountId);
        }

        public async Task DeleteInvitationAsync(Guid eventId, List<Guid> accountIds)
        {
            await _connection.Invitations.DeleteAsync(i => i.EventId == eventId && accountIds.Contains(i.InvitedAccountId));
        }

        public async Task<ListResponse<InvitationDto>> SearchInvitationsAsync(InvitationsSearchRequest request)
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
                .OrderByDescending(i => i.CreationDate)
                .AsQueryable();

            var hasInviterAccounts = request.InviterAccountIds?.Any() ?? false;
            var hasInviterOrgs = request.InviterOrgIds?.Any() ?? false;

            if (hasInviterAccounts || hasInviterOrgs)
            {
                // При поиске «отправленных мной» также включаем приглашения от организаций,
                // в которых указанные аккаунты — активные участники
                var inviterOrganizationIds = hasInviterOrgs
                    ? request.InviterOrgIds!.ToList()
                    : new List<Guid>();

                if (hasInviterAccounts)
                {
                    var memberOrganizationIds = await _connection.OrganizationMembers
                        .Where(m => request.InviterAccountIds!.Contains(m.AccountId) && m.Active)
                        .Select(m => m.OrganizationId)
                        .Distinct()
                        .ToListAsync();

                    foreach (var organizationId in memberOrganizationIds)
                    {
                        if (!inviterOrganizationIds.Contains(organizationId))
                            inviterOrganizationIds.Add(organizationId);
                    }
                }

                invitationsRequest = invitationsRequest.Where(i =>
                    (hasInviterAccounts && request.InviterAccountIds!.Contains(i.InviterAccountId))
                    || (i.InviterOrganizationId != null
                        && inviterOrganizationIds.Contains(i.InviterOrganizationId.Value)));
            }

            if (request.InvitedAccountIds?.Any() ?? false)
                invitationsRequest = invitationsRequest.Where(i => request.InvitedAccountIds.Contains(i.InvitedAccountId));

            if (request.EventIds?.Any() ?? false)
                invitationsRequest = invitationsRequest.Where(i => request.EventIds.Contains(i.EventId));

            var totalCount = invitationsRequest.Count();

            List<InvitationDto> resultList;
            if (request.PageSize != null && request.PageIndex != null)
                resultList = await invitationsRequest.Skip(request.PageSize.Value * request.PageIndex.Value).Take(request.PageSize.Value).ToListAsync();
            else
                resultList = await invitationsRequest.ToListAsync();

            return new ListResponse<InvitationDto>(totalCount, resultList);
        }
    }
}
