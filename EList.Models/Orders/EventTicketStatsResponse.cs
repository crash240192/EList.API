namespace EList.Models.Orders
{
    /// <summary>
    /// Сводка билетов по мероприятию (W6b)
    /// </summary>
    public class EventTicketStatsResponse
    {
        public Guid EventId { get; set; }
        public int Sold { get; set; }
        public int IssuedOpen { get; set; }
        public int Used { get; set; }
        public int RefundPending { get; set; }
        public int Refunded { get; set; }
        public int Void { get; set; }
        /// <summary>Σ quantity заказов Pending+Authorized</summary>
        public int OrdersPending { get; set; }
        /// <summary>Σ quantity Pending+Authorized+Paid (как soft-hold в CreateOrder)</summary>
        public int Reserved { get; set; }
        /// <summary>Остаток мест на уровне события, если задан MaxPersons; иначе null</summary>
        public int? Remaining { get; set; }
        public Dictionary<string, int> ByStatus { get; set; } = new();
        public List<EventTicketTypeStatsItem> ByType { get; set; } = new();
    }
}
