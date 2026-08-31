using EList.Common.Models;
using EList.Common.Support;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class PagingValidator : IPagingValidator
    {
        public int DefaultPageSize => 20;
        public int DefaultMaxPageSize => 100;

        public CommandResult Validate(int? pageIndex, int? pageSize, int? maxPageSize = null)
        {
            var limit = maxPageSize ?? DefaultMaxPageSize;

            if (pageIndex != null && pageIndex < 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Номер страницы не может быть отрицательным");

            if (pageSize != null && pageSize <= 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Размер страницы должен быть больше нуля");

            if (pageSize != null && pageSize > limit)
                return CommandResult.Fail(ErrorCode.InvalidValue, $"Размер страницы не может превышать {limit}");

            return CommandResult.OK;
        }

        public void Normalize(ref int? pageIndex, ref int? pageSize, int? maxPageSize = null, int? defaultPageSize = null)
        {
            var limit = maxPageSize ?? DefaultMaxPageSize;
            var defaultSize = defaultPageSize ?? DefaultPageSize;

            if (pageIndex == null || pageIndex < 0)
                pageIndex = 0;

            if (pageSize == null || pageSize <= 0)
                pageSize = Math.Min(defaultSize, limit);
            else if (pageSize > limit)
                pageSize = limit;
        }
    }
}
