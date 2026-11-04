using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Participation;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/participations")]
    [LoggerHandlerWebApiFilter]
    public class ParticipationController : ControllerBase
    {
        private readonly IParticipationsService _participationService;
        private readonly IDataConnectionProvider _connectionProvider;

        public ParticipationController(IDataConnectionProvider connectionProvider,
            IParticipationsService participationService)
        {
            _participationService = participationService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Участвовать в событии
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("participate/{id}")]
        public async Task<CommandResult> ParticipateAsync(Guid id)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _participationService.ParticipateAsync(id);

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
        /// Покинуть событие
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("leave/{id}")]
        public async Task<CommandResult> LeaveEventAsync(Guid id)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _participationService.LeaveEventAsync(id);

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
        /// Получить список участников события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("eventParticipants")]
        public async Task<CommandResult<PagedList<Participant>>> GetEventParticipantsAsync(EventParticipantsSearchRequest request)
        {
            var result = await _participationService.GetEventParticipantsAsync(request);
            return result;
        }

        #region blackList

        /// <summary>
        /// Получить чёрный список участников события
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("blackList/get/{eventId}")]
        public async Task<CommandResult<PagedList<ParticipantBlackListItem>>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var result = await _participationService.GetEventBlackListAsync(eventId, pageIndex, pageSize);
            return result;

        }

        /// <summary>
        /// Получить белый список участников события
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("whiteList/get/{eventId}")]
        public async Task<CommandResult<PagedList<ParticipantWhiteListItem>>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var result = await _participationService.GetEventWhiteListAsync(eventId, pageIndex, pageSize);
            return result;

        }

        /// <summary>
        /// Черный список участников события
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpGet("blackList/get/{eventId}/short")]
        public async Task<CommandResult<List<Guid>>> GetEventBlackListShortAsync(Guid eventId)
        {
            var result = await _participationService.GetEventBlackListShortAsync(eventId);
            return result;

        }

        /// <summary>
        /// Получить белый список участников события
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpGet("whiteList/get/{eventId}/short")]
        public async Task<CommandResult<List<Guid>>> GetEventWhiteListShortAsync(Guid eventId)
        {
            var result = await _participationService.GetEventWhiteListShortAsync(eventId);
            return result;

        }

        /// <summary>
        /// Добавить пользователя в чёрный список
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("blackList/addUsers")]
        public async Task<CommandResult> AddToBlackListAsync(AddUsersToBWListRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _participationService.AddToBlackListAsync(request);

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
        /// Добавить пользователя в белый список
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("whiteList/addUsers")]
        public async Task<CommandResult> AddToWhiteListAsync(AddUsersToBWListRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _participationService.AddToWhiteListAsync(request);

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
        /// Удалить пользователя из чёрного списка
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpDelete("blackList/deleteUser")]
        public async Task<CommandResult> DeleteFromBlackListAsync(Guid eventId, Guid accountId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _participationService.DeleteFromBlackListAsync(eventId, accountId);

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
        /// Удалить пользователя из белого списка
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpDelete("whiteList/deleteUser")]
        public async Task<CommandResult> DeleteFromWhiteListAsync(Guid eventId, Guid accountId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _participationService.DeleteFromWhiteListAsync(eventId, accountId);

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

        #endregion
    }
}
