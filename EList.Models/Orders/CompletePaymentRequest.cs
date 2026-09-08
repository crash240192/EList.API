namespace EList.Models.Orders
{
    /// <summary>
    /// Подтверждение оплаты для stub-провайдера (имитация webhook ЮKassa).
    /// </summary>
    public class CompletePaymentRequest
    {
        /// <summary>Идентификатор заказа (если известен).</summary>
        public Guid? OrderId { get; set; }

        /// <summary>Идентификатор платежа у провайдера.</summary>
        public string? ProviderPaymentId { get; set; }
    }
}
