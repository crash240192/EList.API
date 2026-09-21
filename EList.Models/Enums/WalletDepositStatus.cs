namespace EList.Models.Enums
{
    /// <summary>
    /// Статус пополнения тарифного кошелька (не билетный контур).
    /// </summary>
    public enum WalletDepositStatus
    {
        Pending = 0,
        Succeeded = 1,
        Canceled = 2,
        Failed = 3
    }
}
