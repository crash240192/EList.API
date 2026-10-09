namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Тип билета на мероприятие (цена / квота / активность).
    /// </summary>
    public class EventTicketType
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "RUB";
        public int? Capacity { get; set; }
        public int SortOrder { get; set; }
        public bool Active { get; set; } = true;
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset UpdateDate { get; set; }
    }
}
