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
        Both = 2,

        [MapValue(Value = "photo")]
        Photo = 3,

        [MapValue(Value = "account")]
        Account = 4,

        [MapValue(Value = "organization")]
        Organization = 5,

        [MapValue(Value = "event_organizator")]
        EventOrganizator = 6,

        [MapValue(Value = "all")]
        All = 7
    }
}
