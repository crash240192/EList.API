using EList.Common.Models;

namespace EList.Validators.Interfaces
{
    public interface IPagingValidator
    {
        int DefaultPageSize { get; }
        int DefaultMaxPageSize { get; }

        CommandResult Validate(int? pageIndex, int? pageSize, int? maxPageSize = null);

        /// <summary>
        /// Применяет значения по умолчанию и ограничивает pageSize сверху.
        /// </summary>
        void Normalize(ref int? pageIndex, ref int? pageSize, int? maxPageSize = null, int? defaultPageSize = null);
    }
}
