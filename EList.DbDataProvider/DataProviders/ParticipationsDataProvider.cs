using EList.Common.Encryption;
using EList.Common.Extensions;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using EList.DbDataProvider.Security;
using EList.Models.Invitations;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class ParticipationsDataProvider : DataProviderBase, IParticipationsDataProvider
    {
        private readonly IFieldEncryptor _fieldEncryptor;

        public ParticipationsDataProvider(
            IDataConnectionProvider dataConnectionProvider,
            IFieldEncryptor fieldEncryptor) : base(dataConnectionProvider)
        {
            _fieldEncryptor = fieldEncryptor;
        }

        public async Task LeaveEventAsync(Guid accountId, Guid eventId)
        {
            var existingParticipation = await _connection.Participations.FirstOrDefaultAsync(i => i.AccountId == accountId && eventId == i.EventId);

            if (existingParticipation != null)
            {
                await _connection.DeleteAsync(existingParticipation);
            }
        }

        public async Task DropParticipationsAsync(Guid eventId, List<Guid> accountIds)
        {
            await _connection.Participations.DeleteAsync(i => i.EventId == eventId && accountIds.Contains(i.AccountId));
        }

        public async Task DropAllParticipationsExceptThisUsersAsync(Guid eventId, List<Guid> accountIds)
        {
            await _connection.Participations.DeleteAsync(i => i.EventId == eventId && !accountIds.Contains(i.AccountId));
        }

        public async Task DropAllParticipationsExceptWhiteListAsync(Guid eventId)
        {
            var whiteList = await _connection.WhiteList.Where(i => i.EventId == eventId).Select(i => i.AccountId).ToListAsync();
            await _connection.Participations.DeleteAsync(i => i.EventId == eventId && !whiteList.Contains(i.AccountId));
        }

        public async Task<Guid> ParticipateAsync(Guid accountId, Guid eventId)
        {
            var existingParticipation = await _connection.Participations.FirstOrDefaultAsync(i => i.AccountId == accountId && eventId == i.EventId);

            Guid result;
            if (existingParticipation == null)
            {
                result = (Guid)await _connection.InsertWithIdentityAsync(new ParticipationDto
                {
                    AccountId = accountId,
                    EventId = eventId
                });
            }
            else
            {
                result = existingParticipation.Id;
            }

            return result;
        }

        public async Task<bool> IsUserParticipatedAsync(Guid accountId, Guid eventId)
        {
            var result = await _connection.Participations.AnyAsync(i => i.AccountId == accountId && eventId == i.EventId);
            return result;
        }

        public async Task<ListResponse<AccountDto>> GetEventParticipantsAsync(EventParticipantsSearchRequest request)
        {
            var accountsRequest = _connection.Participations
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.Avatars)
                .Where(i => request.EventId == i.EventId)
                .OrderBy(i => i.Account.Login)
                .Select(i => i.Account);

            if (request.Gender != null)
                accountsRequest = accountsRequest.Where(i => i.PersonInfo.Gender == request.Gender);

            #region subscriptions
            if (request.SubscribedToId != null) // Список участников, подписанных на этого пользователя
            {
                accountsRequest = accountsRequest.LoadWith(i => i.Subscriptions)
                    .Where(i => i.Subscriptions.Any(s => s.SubscribedToId == request.SubscribedToId));
            }

            if (request.SubscriberId != null) // Список участников на которых подписан этот пользователь
            {
                accountsRequest = accountsRequest.LoadWith(i => i.Subscribers)
                    .Where(i => i.Subscribers.Any(s => s.SubscriberId == request.SubscriberId));
            }
            #endregion

            // ФИО/дата рождения зашифрованы — фильтр и сортировка по ним только после decrypt
            var needsPersonFilter = !string.IsNullOrWhiteSpace(request.Name) || request.Age != null;
            List<AccountDto> resultList;
            int count;

            if (!needsPersonFilter)
            {
                count = await accountsRequest.CountAsync();
                if (request.PageSize != null && request.PageIndex != null)
                    resultList = await accountsRequest.Skip(request.PageSize.Value * request.PageIndex.Value).Take(request.PageSize.Value).ToListAsync();
                else
                    resultList = await accountsRequest.ToListAsync();
            }
            else
            {
                var all = await accountsRequest.ToListAsync();
                foreach (var account in all)
                    PersonalDataCrypto.DecryptPerson(account.PersonInfo, _fieldEncryptor);

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var splitNameSubstrings = request.Name.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    all = all.Where(i => splitNameSubstrings.All(nameItem =>
                        (i.Login?.ToLower().Contains(nameItem) ?? false)
                        || (i.PersonInfo?.FirstName?.ToLower().Contains(nameItem) ?? false)
                        || (i.PersonInfo?.LastName?.ToLower().Contains(nameItem) ?? false)
                        || (i.PersonInfo?.Patronymic?.ToLower().Contains(nameItem) ?? false)
                    )).ToList();
                }

                if (request.Age != null)
                {
                    var minBirth = DateTime.Now.AddYears(-request.Age.Value);
                    all = all.Where(i =>
                    {
                        var birth = PersonalDataCrypto.ParseBirthdate(i.PersonInfo?.Birthdate);
                        return birth != null && birth >= minBirth;
                    }).ToList();
                }

                all = all
                    .OrderBy(i => i.Login)
                    .ThenBy(i => i.PersonInfo?.Patronymic)
                    .ThenBy(i => i.PersonInfo?.LastName)
                    .ThenBy(i => i.PersonInfo?.FirstName)
                    .ToList();

                count = all.Count;
                if (request.PageSize != null && request.PageIndex != null)
                    resultList = all.Skip(request.PageSize.Value * request.PageIndex.Value).Take(request.PageSize.Value).ToList();
                else
                    resultList = all;
            }

            return new ListResponse<AccountDto>(count, resultList);
        }


        public async Task<List<Guid>> GetEventParticipantIdsAsync(Guid eventId)
        {
            var result = await _connection.Participations
                .Where(i => eventId == i.EventId)
                .OrderBy(i => i.Account.Login)
                .Select(i => i.Account.Id)
                .ToListAsync();

            return result;
        }


        public async Task<int> GetParticipantsCountAsync(Guid eventId)
        {
            var count = await _connection.Participations.Where(i => i.EventId == eventId)
                .CountAsync();
            return count;
        }
    }
}
