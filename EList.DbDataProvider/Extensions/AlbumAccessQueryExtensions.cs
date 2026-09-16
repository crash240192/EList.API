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
                // Согласовано с EventAccessValidator: в WL — доступен; пустой WL — только invite/participant.
                if (eventItem.WhiteList.Any(w => w.AccountId == accountId))
                    return true;

                if (eventItem.WhiteList.Any())
                    return false;

                return eventItem.Invitations.Any(inv => inv.InvitedAccountId == accountId)
                    || eventItem.Participants.Any(p => p.AccountId == accountId);
            }

            if (eventItem.BlackList.Any(b => b.AccountId == accountId))
                return false;

            return eventItem.Participants.Any(p => p.AccountId == accountId)
                || eventItem.Invitations.Any(inv => inv.InvitedAccountId == accountId)
                || relation.Album?.Parameters?.Private != true;
        }
    }
}
