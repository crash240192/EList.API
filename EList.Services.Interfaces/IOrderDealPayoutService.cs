namespace EList.Services.Interfaces
{
    /// <summary>
    /// Выплаты из сделки Мультирасчётов (A2C) после успешной оплаты заказа.
    /// </summary>
    public interface IOrderDealPayoutService
    {
        /// <summary>
        /// Идемпотентно: A2C продавцу на amount_seller с FinalPayout (остаток = комиссия площадке).
        /// Ошибки банка логируются и пишутся в журнал; исключение наружу не бросает.
        /// </summary>
        Task TryPayoutSellerAfterPaidAsync(Guid orderId);
    }
}
