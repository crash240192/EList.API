using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Models.SearchRequests
{
    public class EventsSearchRequest
    {
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? LocationRange { get; set; }
        public List<Guid> Types { get; set; }
        public List<Guid> Categories { get; set; }
        public string Name { get; set; }
        public Guid? OrganizatorId { get; set; }
        public Guid? ParticipantId { get; set; }
        public double? Price { get; set; }
        public Gender? AllowedGender { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }  
    }
}
