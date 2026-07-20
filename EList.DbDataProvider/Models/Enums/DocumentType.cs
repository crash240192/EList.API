using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Тип документа
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// Политика обработки ПДн
        /// </summary>
        [MapValue(Value = "policy")]
        Policy = 0,

        /// <summary>
        /// Согласие на обработку ПДн
        /// </summary>
        [MapValue(Value = "consent")]
        Consent = 1,

        /// <summary>
        /// Пользовательское соглашение
        /// </summary>
        [MapValue(Value = "agreement")]
        Agreement = 2
    }
}
