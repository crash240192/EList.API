using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Статус пополнения тарифного кошелька
    /// </summary>
    public enum WalletDepositStatus
    {
        [MapValue(Value = "pending")]
        Pending = 0,

        [MapValue(Value = "succeeded")]
        Succeeded = 1,

        [MapValue(Value = "canceled")]
        Canceled = 2,

        [MapValue(Value = "failed")]
        Failed = 3
    }
}
