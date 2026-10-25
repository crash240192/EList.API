namespace EList.Services.Interfaces
{
    /// <summary>
    /// Выплаты из сделки Мультирасчётов (A2C) и maintenance (ретраи / closeSpDeal).
    /// </summary>
    public interface IOrderDealPayoutService
    {
        /// <summary>
        /// Идемпотентно: A2C продавцу на amount_seller с FinalPayout (остаток = комиссия площадке).
        /// Ошибки банка логируются и пишутся в журнал; исключение наружу не бросает.
        /// </summary>
        Task TryPayoutSellerAfterPaidAsync(Guid orderId);

        /// <summary>
        /// P4: GetState по payout в статусе Initiated → Succeeded/Failed.
        /// </summary>
        Task<int> SyncInitiatedPayoutsAsync(int limit = 50);

        /// <summary>
        /// P4: повтор A2C для Paid+Deal без успешной seller-выплаты (Failed/Skipped/нет записи),
        /// если с момента оплаты прошло ≥ dealTermDays/2 (ранний ретрай) или сразу для Failed.
        /// </summary>
        Task<int> RetryOpenDealPayoutsAsync(int limit = 50);

        /// <summary>
        /// P4: Paid+Deal старше dealTermDays без успешного FinalPayout → closeSpDeal (остаток площадке).
        /// </summary>
        Task<int> CloseExpiredOpenDealsAsync(int limit = 50);
    }
}
