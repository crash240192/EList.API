namespace EList.Models.Events
{
    /// <summary>
    /// Полная замена списка билетёров мероприятия
    /// </summary>
    public class EventTicketStaffUpsertRequest
    {
        public List<EventTicketStaffItemRequest> Staff { get; set; } = new();
    }
}