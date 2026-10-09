namespace EList.Models.Events
{
    /// <summary>
    /// Назначение билетёра на мероприятие
    /// </summary>
    public class EventTicketStaff
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
