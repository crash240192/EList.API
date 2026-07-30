using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Платёжный провайдер
    /// </summary>
    public enum PaymentProvider
    {
        [MapValue(Value = "yookassa")]
        Yookassa = 0,

        [MapValue(Value = "tbank")]
        Tbank = 1,

        [MapValue(Value = "sberpay")]
        Sberpay = 2,

        [MapValue(Value = "payanyway")]
        Payanyway = 3,

        [MapValue(Value = "paygine")]
        Paygine = 4,

        [MapValue(Value = "other")]
        Other = 5
    }
}
