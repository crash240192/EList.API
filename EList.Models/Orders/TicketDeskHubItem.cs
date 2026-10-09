namespace EList.Models.Orders
{
    /// <summary>
    /// Элемент hub «Билеты» / desk: событие, доступное текущему пользователю
    /// </summary>
    public class TicketDeskHubItem
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public bool Active { get; set; }
        public bool TicketsEnabled { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        /// <summary>organizer | staff</summary>
        public string Access { get; set; } = "organizer";
        public bool CanCheckIn { get; set; }
        public bool CanViewStats { get; set; }
        public int? Sold { get; set; }
        public int? IssuedOpen { get; set; }
        public int? Used { get; set; }
        public int? OrdersPending { get; set; }
        public int? Remaining { get; set; }
    }
}
