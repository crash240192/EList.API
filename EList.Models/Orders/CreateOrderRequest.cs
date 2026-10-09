namespace EList.Models.Orders
{
    /// <summary>
    /// Запрос на покупку билетов мероприятия.
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>Идентификатор мероприятия.</summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Тип билета. Обязателен, если у события больше одного активного типа;
        /// при ровно одном типе может быть опущен (подставится автоматически).
        /// </summary>
        public Guid? TicketTypeId { get; set; }

        /// <summary>Количество билетов (по умолчанию 1).</summary>
        public int Quantity { get; set; } = 1;

        /// <summary>Ключ идемпотентности (повторный запрос вернёт тот же заказ).</summary>
        public string? IdempotencyKey { get; set; }
    }
}
