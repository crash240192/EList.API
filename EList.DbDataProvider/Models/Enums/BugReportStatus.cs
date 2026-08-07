using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum BugReportStatus
    {
        [MapValue(Value = "pending")]
        Pending = 0,

        [MapValue(Value = "resolved")]
        Resolved = 1,

        [MapValue(Value = "cancelled")]
        Cancelled = 2
    }
}
