using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Статус билета
    /// </summary>
    public enum TicketStatus
    {
        [MapValue(Value = "issued")]
        Issued = 0,

        [MapValue(Value = "used")]
        Used = 1,

        [MapValue(Value = "refunded")]
        Refunded = 2,

        [MapValue(Value = "void")]
        Void = 3
    }
}
