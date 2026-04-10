using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Тип рейтинга события
    /// </summary>
    public enum EventRatingType
    {
        /// <summary>
        /// Рейтинг ожидания
        /// </summary>
        [MapValue(Value = "expectation")] Expectation,

        /// <summary>
        /// Итоговый рейтинг
        /// </summary>
        [MapValue(Value = "summary")] Summary
    }
}
