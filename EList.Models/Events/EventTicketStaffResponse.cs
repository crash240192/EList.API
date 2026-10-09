namespace EList.Models.Events
{
    /// <summary>
    /// Ответ: билетёр, назначенный на мероприятие
    /// </summary>
    public class EventTicketStaffResponse
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid AccountId { get; set; }
        public bool CanCheckIn { get; set; }
        public bool CanViewStats { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset? UpdateDate { get; set; }
    }
}
