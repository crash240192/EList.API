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
        /// Тип билета
        /// </summary>
        public Guid? TicketTypeId { get; set; }

        /// <summary>
        /// Название типа билета (если известно)
        /// </summary>
        public string? TicketTypeName { get; set; }

        /// <summary>
        /// Идентификатор владельца билета (на desk может скрываться при ticketDeskRevealHolder=false)
        /// </summary>
        public Guid? HolderAccountId { get; set; }

        /// <summary>
        /// Login holder — только если features:ticketDeskRevealHolder=true
        /// </summary>
        public string? HolderLogin { get; set; }

        /// <summary>
        /// Отображаемое имя holder — только если features:ticketDeskRevealHolder=true
        /// </summary>
        public string? HolderDisplayName { get; set; }

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

        /// <summary>
        /// Когда отмечено присутствие
        /// </summary>
        public DateTimeOffset? CheckedInAt { get; set; }

        /// <summary>
        /// Кто отметил присутствие
        /// </summary>
        public Guid? CheckedInByAccountId { get; set; }
    }
}
