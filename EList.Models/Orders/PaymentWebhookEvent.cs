using EList.Models.Enums;

namespace EList.Models.Orders
{
    /// <summary>
    /// Событие webhook от платёжного провайдера
    /// </summary>
    public class PaymentWebhookEvent
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Платёжный провайдер
        /// </summary>
        public PaymentProvider Provider { get; set; }

        /// <summary>
        /// Идентификатор события у провайдера
        /// </summary>
        public string ProviderEventId { get; set; }

        /// <summary>
        /// Связанный заказ
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// Сырое тело колбэка
        /// </summary>
        public string? Payload { get; set; }

        /// <summary>
        /// Дата получения
        /// </summary>
        public DateTimeOffset ReceivedAt { get; set; }

        /// <summary>
        /// Дата обработки
        /// </summary>
        public DateTimeOffset? ProcessedAt { get; set; }
    }
}
