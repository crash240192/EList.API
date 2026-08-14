using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ReportActorContext
    {
        [MapValue(Value = "reporter")]
        Reporter = 0,

        [MapValue(Value = "organizer")]
        Organizer = 1,

        [MapValue(Value = "platform_moderator")]
        PlatformModerator = 2,

        [MapValue(Value = "system")]
        System = 3
    }
}
