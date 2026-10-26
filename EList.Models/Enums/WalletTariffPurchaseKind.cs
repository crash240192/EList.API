namespace EList.Models.Enums
{
    /// <summary>Назначение платежа по тарифному кошельку.</summary>
    public enum WalletTariffPurchaseKind : short
    {
        /// <summary>Устаревшее пополнение баланса (только WalletAutoRenew).</summary>
        TopUp = 0,

        /// <summary>Первая покупка платного тарифа (с free / после истечения).</summary>
        Purchase = 1,

        /// <summary>Продление того же тарифа: activeUntil = max(now, activeUntil) + Period.</summary>
        Extend = 2,

        /// <summary>Повышение тарифа с credit за остаток текущего периода.</summary>
        Upgrade = 3
    }
}
