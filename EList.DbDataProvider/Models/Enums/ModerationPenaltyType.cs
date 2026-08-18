using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ModerationPenaltyType
    {
        [MapValue(Value = "suspend_account")]
        SuspendAccount = 0,

        [MapValue(Value = "suspend_organization")]
        SuspendOrganization = 1,

        [MapValue(Value = "ban_event_create")]
        BanEventCreate = 2,

        [MapValue(Value = "ban_event_participate")]
        BanEventParticipate = 3,

        [MapValue(Value = "ban_messaging")]
        BanMessaging = 4,

        [MapValue(Value = "ban_organize")]
        BanOrganize = 5,

        [MapValue(Value = "ban_from_event")]
        BanFromEvent = 6
    }
}
