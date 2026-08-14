using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ReportResolutionAction
    {
        [MapValue(Value = "hide_content")]
        HideContent = 0,

        [MapValue(Value = "delete_content")]
        DeleteContent = 1,

        [MapValue(Value = "warn")]
        Warn = 2,

        [MapValue(Value = "ban_from_event")]
        BanFromEvent = 3,

        [MapValue(Value = "cancel_event")]
        CancelEvent = 4,

        [MapValue(Value = "dismiss")]
        Dismiss = 5,

        [MapValue(Value = "escalate")]
        Escalate = 6,

        [MapValue(Value = "other")]
        Other = 7
    }
}
