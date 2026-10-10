namespace EList.Models.Orders
{
    /// <summary>
    /// Краткая сводка билетов события для hub организации (W6b)
    /// </summary>
    public class OrganizationEventTicketSummaryItem
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public bool Active { get; set; }
        public bool TicketsEnabled { get; set; }
        public int Sold { get; set; }
        public int IssuedOpen { get; set; }
        public int Used { get; set; }
        public int OrdersPending { get; set; }
        public int? Remaining { get; set; }
    }
}
