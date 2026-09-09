using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.Events;

namespace EList.Models.Orders
{
    /// <summary>
    /// Билет
    /// </summary>
    public class Ticket
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

        /// <summary>
        /// Когда организатор отметил присутствие (check-in).
        /// </summary>
        public DateTimeOffset? CheckedInAt { get; set; }

        /// <summary>
        /// Кто отметил присутствие (организатор / staff).
        /// </summary>
        public Guid? CheckedInByAccountId { get; set; }

        /// <summary>
        /// Заказ
        /// </summary>
        public Order? Order { get; set; }

        /// <summary>
        /// Мероприятие
        /// </summary>
        public Event? Event { get; set; }

        /// <summary>
        /// Владелец билета
        /// </summary>
        public AccountPublicData? HolderAccount { get; set; }

        /// <summary>
        /// Кто сделал check-in
        /// </summary>
        public AccountPublicData? CheckedInByAccount { get; set; }
    }
}
