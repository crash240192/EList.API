using EList.Models.Enums;

namespace EList.Models.Orders
{
    /// <summary>
    /// Ответ API с данными заказа
    /// </summary>
    public class OrderResponse
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор мероприятия
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Идентификатор покупателя
        /// </summary>
        public Guid BuyerAccountId { get; set; }

        /// <summary>
        /// Идентификатор организации-продавца
        /// </summary>
        public Guid SellerOrganizationId { get; set; }

        /// <summary>
        /// Количество билетов
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Полная сумма заказа
        /// </summary>
        public decimal AmountTotal { get; set; }

        /// <summary>
        /// Доля продавца
        /// </summary>
        public decimal AmountSeller { get; set; }

        /// <summary>
        /// Комиссия сервиса
        /// </summary>
        public decimal AmountCommission { get; set; }

        /// <summary>
        /// Валюта
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Статус заказа
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Дата оплаты
        /// </summary>
        public DateTimeOffset? PaidAt { get; set; }

        /// <summary>
        /// Билеты заказа
        /// </summary>
        public List<TicketResponse>? Tickets { get; set; }
    }
}
