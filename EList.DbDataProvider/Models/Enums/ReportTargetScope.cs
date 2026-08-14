using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// К каким типам контента применима причина жалобы.
    /// </summary>
    public enum ReportTargetScope
    {
        [MapValue(Value = "event")]
        Event = 0,

        [MapValue(Value = "message")]
        Message = 1,

        [MapValue(Value = "both")]
        Both = 2
    }
}
