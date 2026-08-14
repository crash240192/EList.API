using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum ReportSeverity
    {
        [MapValue(Value = "community")]
        Community = 0,

        [MapValue(Value = "safety")]
        Safety = 1
    }
}
