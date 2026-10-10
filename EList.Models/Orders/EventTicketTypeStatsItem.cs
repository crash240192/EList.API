namespace EList.Models.Orders
{
    /// <summary>
    /// Агрегаты билетов по типу (W6b)
    /// </summary>
    public class EventTicketTypeStatsItem
    {
        public Guid? TicketTypeId { get; set; }
        public string TicketTypeName { get; set; } = string.Empty;
        public int Sold { get; set; }
        public int IssuedOpen { get; set; }
        public int Used { get; set; }
        public int RefundPending { get; set; }
        public int Refunded { get; set; }
        public int Void { get; set; }
        public int OrdersPending { get; set; }
        public int Reserved { get; set; }
        public int? Capacity { get; set; }
        public int? Remaining { get; set; }
    }
}
