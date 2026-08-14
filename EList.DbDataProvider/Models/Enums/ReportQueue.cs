using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ReportQueue
    {
        [MapValue(Value = "organizers")]
        Organizers = 0,

        [MapValue(Value = "platform")]
        Platform = 1,

        [MapValue(Value = "both")]
        Both = 2
    }
}
