using EList.Models.Enums;

namespace EList.Models.Orders
{
    /// <summary>
    /// Ответ API с данными билета
    /// </summary>
    public class TicketResponse
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор заказа
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Идентификатор мероприятия
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Идентификатор владельца билета
        /// </summary>
        public Guid HolderAccountId { get; set; }

        /// <summary>
        /// Статус билета
        /// </summary>
        public TicketStatus Status { get; set; }

        /// <summary>
        /// Уникальный код / QR билета
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Дата выдачи
        /// </summary>
        public DateTimeOffset IssuedAt { get; set; }
    }
}
