using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Статус онбординга организации у платёжного провайдера
    /// </summary>
    public enum ProviderOnboardingStatus
    {
        [MapValue(Value = "none")]
        None = 0,

        [MapValue(Value = "pending")]
        Pending = 1,

        [MapValue(Value = "active")]
        Active = 2,

        [MapValue(Value = "rejected")]
        Rejected = 3
    }
}
