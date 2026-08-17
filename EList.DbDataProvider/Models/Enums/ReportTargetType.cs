using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ReportTargetType
    {
        [MapValue(Value = "event")]
        Event = 0,

        [MapValue(Value = "message")]
        Message = 1,

        [MapValue(Value = "photo")]
        Photo = 2,

        [MapValue(Value = "account")]
        Account = 3,

        [MapValue(Value = "organization")]
        Organization = 4,

        [MapValue(Value = "event_organizator")]
        EventOrganizator = 5
    }
}
