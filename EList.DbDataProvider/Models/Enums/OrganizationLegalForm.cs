using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Юридическая форма организации
    /// </summary>
    public enum OrganizationLegalForm
    {
        /// <summary>
        /// Самозанятый
        /// </summary>
        [MapValue(Value = "self_employed")]
        SelfEmployed = 0,

        /// <summary>
        /// ИП
        /// </summary>
        [MapValue(Value = "ip")]
        Ip = 1,

        /// <summary>
        /// Юридическое лицо
        /// </summary>
        [MapValue(Value = "legal_entity")]
        LegalEntity = 2
    }
}
