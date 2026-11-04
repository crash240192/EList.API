using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Enums;
using EList.Models.EventsRating;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Контроллер для работы с рейтингом мероприятий и организаторов
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [LoggerHandlerWebApiFilter]
    public class RatingController : ControllerBase
    {
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IEventsRatingService _eventsRatingService;

        /// <summary>
        /// Конструктор контроллера рейтинга мероприятий и организаторов
        /// </summary>
        /// <param name="correlationIdProvider"></param>
        /// <param name="connectionProvider"></param>
        /// <param name="eventsRatingService"></param>
        public RatingController(IDataConnectionProvider connectionProvider, 
            IEventsRatingService eventsRatingService)
        {
            _connectionProvider = connectionProvider;
            _eventsRatingService = eventsRatingService;
        }
        
        /// <summary>
        /// Проголосовать за мероприятие
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("events/vote")]
        public async Task<CommandResult<Guid>> VoteAsync(EventsRatingItem request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsRatingService.VoteAsync(request);

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
        /// Рейтинг мероприятия
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="eventRatingType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("events/getRating")]
        public async Task<CommandResult<EventRating>> GetEventRatingAsync(Guid eventId, EventRatingType eventRatingType, int? pageIndex = 0, int? pageSize = 20)
        {
            var result = await _eventsRatingService.GetEventRatingAsync(eventId, eventRatingType, pageIndex, pageSize);
            return result;
        }

        /// <summary>
        /// Рейтинг организатора
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("organizators/{accountId}")]
        public async Task<CommandResult<double?>> GetOrganizatorRatingAsync(Guid accountId)
        {
            var result = await _eventsRatingService.GetOrganizatorRatingAsync(accountId);
            return result;

        }

        /// <summary>
        /// Рейтинг организации-организатора (средняя оценка мероприятий от её имени)
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        [HttpGet("organizations/{organizationId}")]
        public async Task<CommandResult<double?>> GetOrganizationRatingAsync(Guid organizationId)
        {
            var result = await _eventsRatingService.GetOrganizationRatingAsync(organizationId);
            return result;

        }

        /// <summary>
        /// Удалить оценку
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpDelete("events/delete/{itemId}")]
        public async Task<CommandResult> DeleteAsync(Guid itemId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsRatingService.DeleteAsync(itemId);

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
    }
}
