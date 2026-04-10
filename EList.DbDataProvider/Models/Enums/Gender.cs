using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Пол
    /// </summary>
    public enum Gender
    {
        /// <summary>
        /// Жен.
        /// </summary>
        [MapValue(Value = "female")]
        Female = 0,

        /// <summary>
        /// Муж.
        /// </summary>
        [MapValue(Value = "male")]
        Male = 1
    }
}
