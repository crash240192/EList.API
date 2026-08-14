using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ReportStatus
    {
        [MapValue(Value = "open")]
        Open = 0,

        [MapValue(Value = "in_review")]
        InReview = 1,

        [MapValue(Value = "resolved")]
        Resolved = 2,

        [MapValue(Value = "dismissed")]
        Dismissed = 3,

        [MapValue(Value = "escalated")]
        Escalated = 4
    }
}
