using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Media;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class MediaAlbumValidator : IMediaAlbumValidator
    {
        public const int MaxNameLength = 255;
        public const int MaxDescriptionLength = 32000;
        public const int MaxFilesPerRequest = 100;

        public CommandResult ValidateAlbumRequest(EventAlbumRequest? request, bool requireName = true)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Данные альбома не указаны");

            if (requireName && string.IsNullOrWhiteSpace(request.Name))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Название альбома не указано");

            var nameError = ValidationCommon.ValidateOptionalTextLength(request.Name, MaxNameLength, "Название альбома");
            if (!nameError.Success)
                return nameError;

            var descriptionError = ValidationCommon.ValidateOptionalTextLength(
                request.Description, MaxDescriptionLength, "Описание альбома");
            if (!descriptionError.Success)
                return descriptionError;

            return CommandResult.OK;
        }

        public CommandResult ValidateAddFilesRequest(AddFilesRequest? request)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Запрос на добавление файлов не указан");

            if (request.AlbumId == Guid.Empty)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Не указан идентификатор альбома");

            return ValidationCommon.ValidateGuidList(
                request.FileIds,
                "Список файлов",
                MaxFilesPerRequest,
                allowEmpty: false,
                requireAtLeastOne: true);
        }
    }
}
