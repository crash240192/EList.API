namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Создание / обновление типа билета в составе assign parameters / create event.
    /// </summary>
    public class EventTicketTypeRequest
    {
        /// <summary>Существующий id для update; null — создать новый.</summary>
        public Guid? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Цена за один билет (≥ 0).</summary>
        public decimal Price { get; set; }

        /// <summary>Лимит мест типа; null — без лимита типа.</summary>
        public int? Capacity { get; set; }

        public int SortOrder { get; set; }

        public bool Active { get; set; } = true;
    }
}
