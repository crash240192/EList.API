using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Models.SearchRequests
{
    public class EventParticipantsSearchRequest
    {
        public Guid EventId { get; set; }
        
        /// <summary>
        /// Идентификатор подписавшегося пользователя
        /// </summary>
        public Guid? SubscriberId { get; set; }
        
        /// <summary>
        /// Идентификатор пользователя на которого подписаны
        /// </summary>
        public Guid? SubscribedToId { get; set; }
        public string Name { get; set; }
        public Gender? Gender { get; set; }
        public int? Age { get; set; }
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 20;
    }
}
