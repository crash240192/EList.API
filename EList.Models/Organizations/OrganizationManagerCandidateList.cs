using EList.Models.Subscriptions;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Слияние подписок и подписчиков в один список без дублей по accountId.
    /// </summary>
    public static class OrganizationManagerCandidateList
    {
        public static List<OrganizationManagerCandidate> MergeUnique(
            IEnumerable<Subscription>? subscriptions,
            IEnumerable<Subscription>? subscribers,
            ISet<Guid> excludeAccountIds)
        {
            var byId = new Dictionary<Guid, OrganizationManagerCandidate>();

            foreach (var item in subscriptions ?? Enumerable.Empty<Subscription>())
                TryAdd(byId, item.SubscribedTo, excludeAccountIds);

            foreach (var item in subscribers ?? Enumerable.Empty<Subscription>())
                TryAdd(byId, item.Subscriber, excludeAccountIds);

            return byId.Values
                .OrderBy(i => i.PersonInfo?.LastName)
                .ThenBy(i => i.PersonInfo?.FirstName)
                .ThenBy(i => i.Account?.Login)
                .ToList();
        }

        private static void TryAdd(
            IDictionary<Guid, OrganizationManagerCandidate> byId,
            Subscriber? person,
            ISet<Guid> excludeAccountIds)
        {
            var accountId = person?.Account?.Id ?? Guid.Empty;
            if (accountId == Guid.Empty || excludeAccountIds.Contains(accountId) || byId.ContainsKey(accountId))
                return;

            byId[accountId] = new OrganizationManagerCandidate
            {
                AccountId = accountId,
                Account = person!.Account,
                PersonInfo = person.PersonInfo
            };
        }
    }
}
