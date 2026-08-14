using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ReportTargetType
    {
        [MapValue(Value = "event")]
        Event = 0,

        [MapValue(Value = "message")]
        Message = 1
    }
}
