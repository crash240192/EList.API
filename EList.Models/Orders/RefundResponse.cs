using EList.Models.Enums;

namespace EList.Models.Orders
{
    /// <summary>
    /// Ответ API с данными возврата
    /// </summary>
    public class RefundResponse
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
        /// Сумма возврата
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Причина возврата
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Статус возврата
        /// </summary>
        public RefundStatus Status { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
    }
}
