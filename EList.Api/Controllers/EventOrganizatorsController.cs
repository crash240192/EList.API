using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.EventOrganizators;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [LoggerHandlerWebApiFilter]
    public class EventOrganizatorsController : ControllerBase
    {
        private readonly IEventOrganizatorsService _organizatorsService;
        private readonly IDataConnectionProvider _connectionProvider;

        public EventOrganizatorsController(IDataConnectionProvider connectionProvider,
            IEventOrganizatorsService organizatorsService
            )
        {
            _organizatorsService = organizatorsService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Получить запись организатора по id
        /// </summary>
        [HttpGet("get/{id}")]
        public async Task<CommandResult<EventOrganizator?>> GetByIdAsync(Guid id)
        {
            var result = await _organizatorsService.GetByIdAsync(id);
            return result;

        }

        /// <summary>
        /// список организаторов мероприятия
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpGet("getByEventId/{eventId}")]
        public async Task<CommandResult<List<EventOrganizator>>> GetByEventIdAsync(Guid eventId)
        {
            var result = await _organizatorsService.GetByEventIdAsync(eventId);
            return result;

        }

        /// <summary>
        /// Добавить мероприятию организаторов
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("assign")]
        public async Task<CommandResult> AssignEventOrganizatorsAsync(EventOrganizatorsListRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizatorsService.AssignEventOrganizatorsAsync(request.EventId, request.AccountIds, request.OrganizationIds);

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
        /// Удалить организатора из мероприятия
        /// </summary>
        [HttpDelete("{eventId}/remove/{organizatorId}")]
        public async Task<CommandResult> RemoveOrganizatorAsync(Guid eventId, Guid organizatorId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _organizatorsService.RemoveOrganizatorAsync(eventId, organizatorId);

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
        /// Проверяет, является ли текущий пользователь организатором мероприятия
        /// (напрямую или как активный участник организации-организатора)
        /// </summary>
        /// <param name="eventId">Идентификатор мероприятия</param>
        [HttpGet("isOrganizator/{eventId}")]
        public async Task<CommandResult<bool>> IsCurrentUserEventOrganizatorAsync(Guid eventId)
        {
            var result = await _organizatorsService.IsCurrentUserEventOrganizatorAsync(eventId);
            return result;
        }
    }
}
