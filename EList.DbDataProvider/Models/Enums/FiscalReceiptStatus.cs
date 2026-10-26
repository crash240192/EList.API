using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum FiscalReceiptStatus
    {
        [MapValue(Value = "pending")]
        Pending = 0,

        [MapValue(Value = "queued")]
        Queued = 1,

        [MapValue(Value = "processed")]
        Processed = 2,

        [MapValue(Value = "failed")]
        Failed = 3,

        [MapValue(Value = "skipped")]
        Skipped = 4
    }
}
