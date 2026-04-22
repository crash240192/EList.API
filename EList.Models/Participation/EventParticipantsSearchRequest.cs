using EList.Models.Enums;

namespace EList.Models.Participation
{
    public class EventParticipantsSearchRequest
    {
        public Guid EventId { get; set; }        
        public Guid? SubscriberId { get; set; }
        public Guid? SubscribedToId { get; set; }
        public string Name { get; set; }
        public Gender? Gender { get; set; }
        public int? Age { get; set; }
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 20;
    }
}
