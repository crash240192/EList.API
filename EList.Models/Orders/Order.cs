using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Organizations;

namespace EList.Models.Orders
{
    /// <summary>
    /// Заказ на покупку билетов
    /// </summary>
    public class Order
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
        /// Платёжный провайдер
        /// </summary>
        public PaymentProvider? Provider { get; set; }

        /// <summary>
        /// Идентификатор платежа у провайдера
        /// </summary>
        public string? ProviderPaymentId { get; set; }

        /// <summary>
        /// Ключ идемпотентности
        /// </summary>
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Дата оплаты
        /// </summary>
        public DateTimeOffset? PaidAt { get; set; }

        /// <summary>
        /// Мероприятие
        /// </summary>
        public Event? Event { get; set; }

        /// <summary>
        /// Покупатель
        /// </summary>
        public AccountPublicData? BuyerAccount { get; set; }

        /// <summary>
        /// Организация-продавец
        /// </summary>
        public Organization? SellerOrganization { get; set; }

        /// <summary>
        /// Билеты заказа
        /// </summary>
        public List<Ticket>? Tickets { get; set; }

        /// <summary>
        /// Возвраты по заказу
        /// </summary>
        public List<Refund>? Refunds { get; set; }
    }
}
