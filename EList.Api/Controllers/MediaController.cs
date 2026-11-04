using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Media;
using EList.Services.Impl;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/media")]
    [LoggerHandlerWebApiFilter]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly IDataConnectionProvider _connectionProvider;

        public MediaController(IMediaService mediaService, IDataConnectionProvider connectionProvider)
        {
            _mediaService = mediaService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Создание альбома
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("albums/create")]
        public async Task<CommandResult<Guid?>> CreateAlbumAsync(EventAlbumRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.CreateAlbumAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Создание альбома
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("albums/update")]
        public async Task<CommandResult> UpdateAlbumAsync(EventAlbumRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.UpdateAlbumAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Связать событие и альбом
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="albumId"></param>
        /// <returns></returns>
        [HttpGet("albums/assign/toEvent")]
        public async Task<CommandResult> AssignAlbumToEventAsync(Guid eventId, Guid albumId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.AssignAlbumToEventAsync(eventId, albumId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Связать аккаунт и альбом
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="albumId"></param>
        /// <returns></returns>
        [HttpGet("albums/assign/toAccount")]
        public async Task<CommandResult> AssignAlbumToAccountAsync(Guid accountId, Guid albumId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.AssignAlbumToAccountAsync(accountId, albumId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Получить альбом по id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("albums/get/{id}")]
        public async Task<CommandResult<MediaAlbum>> GetAlbumAsync(Guid id)
        {
            var result = await _mediaService.GetAlbumAsync(id);
            return result;
        }

        /// <summary>
        /// Получить альбомы аккаунта
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("albums/byAccount/{id}")]
        public async Task<CommandResult<List<MediaAlbum>>> GetAccountAlbumsAsync(Guid id)
        {
            var result = await _mediaService.GetAccountAlbumsAsync(id);
            return result;

        }

        /// <summary>
        /// Возвращает список файлов из указанного альбома
        /// </summary>
        /// <param name="albumId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("albums/filesByAlbumId/{albumId}")]
        public async Task<CommandResult<PagedList<AlbumFile>>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null)
        {
            var result = await _mediaService.GetAlbumFilesAsync(albumId, pageIndex, pageSize);
            return result;

        }

        /// <summary>
        /// Добавить файлы в альбом
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("albums/addFiles")]
        public async Task<CommandResult> AddFilesToAlbumAsync(AddFilesRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.AddFilesToAlbumAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Отметить альбом просмотренным текущим пользователем (сбрасывает HasNewContent).
        /// </summary>
        [HttpPost("albums/{albumId}/markViewed")]
        public async Task<CommandResult> MarkAlbumViewedAsync(Guid albumId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.MarkAlbumViewedAsync(albumId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Получить список альбомов события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("albums/byEvent/{id}")]
        public async Task<CommandResult<List<MediaAlbum>>> GetEventAlbumsAsync(Guid id)
        {
            var result = await _mediaService.GetEventAlbumsAsync(id);
            return result;
        }

        /// <summary>
        /// Получить список альбомов по мероприятиям.
        /// sort: date (StartTime ASC) | latestPhotos (max file create_date DESC) | name.
        /// name: подстрока по названию мероприятия и/или альбома (и описанию альбома).
        /// </summary>
        [HttpGet("albums/byEvents")]
        public async Task<CommandResult<PagedList<EventAlbumsContainer>>> GetEventsAlbumsAsync(
            Guid accountId, int? pageIndex = null, int? pageSize = null, string? sort = null, string? name = null)
        {
            var result = await _mediaService.GetEventsAlbumsAsync(accountId, pageIndex, pageSize, sort, name);
            return result;

        }

        //[HttpPost("albums/setParameters")]
        //public async Task<CommandResult> SetEventAlbumParameters(EventAlbumParameters request)
        //{
        //        //        //
        //    try
        //    {
        //        await _connectionProvider.StartNewTransactionAsync();
        //
        //        var result = await _mediaService.SetEventAlbumParametersAsync(request);
        //        if (!result.Success)
        //        {
        //            await _connectionProvider.RollbackTransactionAsync();
        //            return CommandResult<List<MediaAlbum>>.Fail(result.ErrorCode, result.Message);
        //        }

        //        await _connectionProvider.CommitTransactionAsync();

        //
        //        return result;
        //    }
        //    catch
        //    {
        //        //        throw;
        //    }
        //}

        /// <summary>
        /// Удаление альбома
        /// </summary>
        /// <param name="albumId"></param>
        /// <returns></returns>
        [HttpDelete("albums/{albumId}")]
        public async Task<CommandResult> DeleteAlbumAsync([FromRoute] Guid albumId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.DeleteAlbumAsync(albumId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Удаление файла из альбома
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="albumId"></param>
        /// <returns></returns>
        [HttpDelete("albums/deleteFiles")]
        public async Task<CommandResult> DeleteFileAsync(DeleteFilesRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.DeleteFilesAsync(request.FileIds, request.AlbumId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        #region account avatars
        /// <summary>
        /// Добавление новой аватарки
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        [HttpGet("account/avatars/setNew/{photoId}")]
        public async Task<CommandResult> SetNewAccountAvatarAsync(Guid photoId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.SetNewAccountAvatarAsync(photoId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Получение списка аватарок пользователя
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("account/avatars/getByAccountId/{accountId}")]
        public async Task<CommandResult<List<Guid>?>> GetAccountAvatarsAsync(Guid accountId)
        {
            var result = await _mediaService.GetAccountAvatarsAsync(accountId);
            return result;
        }

        /// <summary>
        /// Получение списка аватарок пользователя
        /// </summary>
        /// <returns></returns>
        [HttpGet("account/avatars/get")]
        public async Task<CommandResult<List<Guid>?>> GetCurAccountAvatarsAsync()
        {
            var result = await _mediaService.GetCurAccountAvatarsAsync();
            return result;

        }

        /// <summary>
        /// Получение последней аватарки пользователя
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("account/avatar/getByAccountId/{accountId}")]
        public async Task<CommandResult<Guid?>> GetAccountAvatarAsync(Guid accountId)
        {
            var result = await _mediaService.GetAccountAvatarAsync(accountId);
            return result;

        }

        /// <summary>
        /// Получение последней аватарки пользователя
        /// </summary>
        /// <returns></returns>
        [HttpGet("account/avatar/get")]
        public async Task<CommandResult<Guid?>> GetCurAccountAvatarAsync()
        {
            var result = await _mediaService.GetCurAccountAvatarAsync();
            return result;

        }

        /// <summary>
        /// Получение списка аватарок пользователя
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        [HttpDelete("account/avatars/delete/{fileId}")]
        public async Task<CommandResult> DeleteAccountAvatarAsync(Guid fileId)
        {
            var result = await _mediaService.DeleteAvatarAsync(fileId);
            return result;

        }
        #endregion acccount avatars

        #region organization avatars
        /// <summary>
        /// Добавление новой аватарки для организации
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="photoId"></param>
        /// <returns></returns>
        [HttpGet("organization/avatars/setNew")]
        public async Task<CommandResult> SetNewOrganizationAvatarAsync(Guid organizationId, Guid photoId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _mediaService.SetNewOrganizationAvatarAsync(organizationId, photoId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Получение списка аватарок организации
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        [HttpGet("organization/avatars/getByOrganizationId/{organizationId}")]
        public async Task<CommandResult<List<Guid>?>> GetOrganizationAvatarsAsync(Guid organizationId)
        {
            var result = await _mediaService.GetOrganizationAvatarsAsync(organizationId);
            return result;
        }

        /// <summary>
        /// Получение последней аватарки организации
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        [HttpGet("organization/avatar/getByOrganizationId/{organizationId}")]
        public async Task<CommandResult<Guid?>> GetOrganizationAvatarAsync(Guid organizationId)
        {
            var result = await _mediaService.GetOrganizationAvatarAsync(organizationId);
            return result;

        }
        #endregion 
    }
}
