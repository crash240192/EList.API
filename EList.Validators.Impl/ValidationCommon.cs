using EList.Common.Models;
using EList.Common.Support;

namespace EList.Validators.Impl
{
    internal static class ValidationCommon
    {
        public static CommandResult ValidateGuidList(
            List<Guid>? ids,
            string fieldName,
            int? maxCount = null,
            bool allowEmpty = true,
            bool requireAtLeastOne = false)
        {
            if (ids == null)
            {
                if (requireAtLeastOne)
                    return CommandResult.Fail(ErrorCode.IsNullOrEmpty, $"{fieldName} не указан");
                return CommandResult.OK;
            }

            if (requireAtLeastOne && ids.Count == 0)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, $"{fieldName} не должен быть пустым");

            if (!allowEmpty && ids.Count == 0)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, $"{fieldName} не должен быть пустым");

            if (maxCount != null && ids.Count > maxCount.Value)
                return CommandResult.Fail(ErrorCode.InvalidValue, $"{fieldName}: слишком много элементов (максимум {maxCount.Value})");

            if (ids.Any(i => i == Guid.Empty))
                return CommandResult.Fail(ErrorCode.InvalidValue, $"{fieldName} содержит некорректный идентификатор");

            if (ids.Count != ids.Distinct().Count())
                return CommandResult.Fail(ErrorCode.InvalidValue, $"{fieldName} содержит дубликаты");

            return CommandResult.OK;
        }

        public static CommandResult ValidateOptionalTextLength(string? value, int maxLength, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CommandResult.OK;

            if (value.Trim().Length > maxLength)
                return CommandResult.Fail(ErrorCode.InvalidValue, $"{fieldName} слишком длинное (максимум {maxLength} символов)");

            return CommandResult.OK;
        }
    }
}
