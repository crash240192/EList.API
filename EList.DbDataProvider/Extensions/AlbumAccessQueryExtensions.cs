using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Extensions
{
    /// <summary>
    /// Правила доступа к альбомам мероприятия.
    /// Для SQL-запросов логику нужно дублировать inline — LinqToDB не переводит вызовы методов.
    /// </summary>
    public static class AlbumAccessQueryExtensions
    {
        public static bool IsAlbumAccessible(EventDto eventItem, EventAlbumRelationDto relation, Guid accountId)
        {
            if (eventItem.Organizators.Any(o => o.AccountId == accountId))
                return true;

            if (eventItem.Parameters?.Private == true)
            {
                var whiteListAllowed = eventItem.WhiteList.Any(w => w.AccountId == accountId)
                    || !eventItem.WhiteList.Any();
                var participantOrInvited = eventItem.Invitations.Any(inv => inv.InvitedAccountId == accountId)
                    || eventItem.Participants.Any(p => p.AccountId == accountId);

                return whiteListAllowed && participantOrInvited;
            }

            if (eventItem.BlackList.Any(b => b.AccountId == accountId))
                return false;

            return eventItem.Participants.Any(p => p.AccountId == accountId)
                || eventItem.Invitations.Any(inv => inv.InvitedAccountId == accountId)
                || relation.Album?.Parameters?.Private != true;
        }
    }
}
