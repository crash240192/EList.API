using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
using EList.Models.Orders;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/events")]
    [LoggerHandlerWebApiFilter]
    public class EventsController : ControllerBase
    {
        private readonly IEventsService _eventsService;
        private readonly IOrdersService _ordersService;
        private readonly IDataConnectionProvider _connectionProvider;

        public EventsController(IEventsService eventsService,
            IOrdersService ordersService,
            IDataConnectionProvider connectionProvider)
        {
            _eventsService = eventsService;
            _ordersService = ordersService;
            _connectionProvider = connectionProvider;
        }
        
        #region eventCategories
        /// <summary>
        /// Создание новой категории
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("eventCategories/create")]
        public async Task<CommandResult<Guid?>> CreateEventCategoryAsync(EventCategoryRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.CreateEventCategoryAsync(request);
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
        /// Обновление категории
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("eventCategories/update/{id}")]
        public async Task<CommandResult> UpdateEventCategoryAsync([FromRoute] Guid id, [FromBody] EventCategoryRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.UpdateEventCategoryAsync(id, request);
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
        /// Удаление категории
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete("eventCategories/delete/{id}")]
        public async Task<CommandResult> DeleteEventCategoryAsync([FromRoute] Guid id)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.DeleteEventCategoryAsync(id);
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
        /// Получение категории события по id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("eventCategories/get/{id}")]
        public async Task<CommandResult<EventCategory?>> GetEventCategoryAsync(Guid id)
        {
            var result = await _eventsService.GetEventCategoryAsync(id);
            return result;
        }

        /// <summary>
        /// Получение списка категорий
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("eventCategories/getAll")]
        public async Task<CommandResult<List<EventCategory>>> GetEventCategoriesAsync()
        {
            var result = await _eventsService.GetAllEventCategoriesAsync();
            return result;

        }
        #endregion

        #region eventTypes
        /// <summary>
        /// Создание новой типа события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("eventTypes/create")]
        public async Task<CommandResult<Guid?>> CreateEventTypeAsync(EventTypeRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.CreateEventTypeAsync(request);
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
        /// Обновление типа события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("eventTypes/update/{id}")]
        public async Task<CommandResult> UpdateEventTypeAsync([FromRoute] Guid id, [FromBody] EventTypeRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.UpdateEventTypeAsync(id, request);
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
        /// Удаление типа события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete("eventTypes/delete/{id}")]
        public async Task<CommandResult> DeleteEventTypeAsync([FromRoute] Guid id)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.DeleteEventTypeAsync(id);
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
        /// Получение типа события по id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("eventTypes/get/{id}")]
        public async Task<CommandResult<EventType?>> GetEventTypeAsync(Guid id)
        {
            var result = await _eventsService.GetEventTypeAsync(id);
            return result;
        }

        /// <summary>
        /// Получение типов событий по id категории
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("eventTypes/byCategoryId/{id}")]
        public async Task<CommandResult<List<EventType>?>> GetEventTypesByCategoryIdAsync(Guid categoryId)
        {
            var result = await _eventsService.GetEventTypesByCategoryIdAsync(categoryId);
            return result;

        }

        /// <summary>
        /// Получение типа события для указанного события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("eventTypes/byEvent/{eventId}")]
        public async Task<CommandResult<List<EventType>?>> GetEventTypeByEventIdAsync(Guid eventId)
        {
            var result = await _eventsService.GetEventTypesByEventIdAsync(eventId);
            return result;

        }

        /// <summary>
        /// Получение списка типов событий
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("eventTypes/getAll")]
        public async Task<CommandResult<List<EventType>>> GetEventTypesAsync()
        {
            var result = await _eventsService.GetAllEventTypesAsync();
            return result;

        }

        /// <summary>
        /// Назначить событию список типов
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="typeids"></param>
        /// <returns></returns>
        [HttpPost("eventTypes/assignToEvent/{eventId}")]
        public async Task<CommandResult> SetEventTypesAsync(Guid eventId, [FromBody] List<Guid> typeids)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.SetEventTypesAsync(eventId, typeids);
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

        #region eventParameters
        /// <summary>
        /// Получить параметры события
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpGet("eventParameters/byEvent/{eventId}")]
        public async Task<CommandResult<EventParameters?>> GetEventParametersByEventIdAsync(Guid eventId)
        {
            var result = await _eventsService.GetEventParametersByEventIdAsync(eventId);
            return result;
        }

        /// <summary>
        /// Типы билетов мероприятия (активные; организатор может запросить includeInactive=true).
        /// </summary>
        [HttpGet("{eventId}/ticket-types")]
        public async Task<CommandResult<List<EventTicketType>?>> GetEventTicketTypesAsync(
            Guid eventId,
            [FromQuery] bool includeInactive = false)
        {
            var result = await _eventsService.GetEventTicketTypesAsync(eventId, includeInactive);
            return result;

        }

        /// <summary>
        /// Hub «Билеты»: события, доступные текущему пользователю для desk
        /// </summary>
        [HttpGet("ticket-desk/hub")]
        public async Task<CommandResult<List<TicketDeskHubItem>>> GetMyTicketDeskHubAsync(
            [FromQuery] int limit = 100)
        {
            var result = await _ordersService.GetMyTicketDeskHubAsync(limit);
            return result;

        }

        /// <summary>
        /// Сводка билетов мероприятия (продано / на площадке / остаток / byType)
        /// </summary>
        [HttpGet("{eventId}/tickets/stats")]
        public async Task<CommandResult<EventTicketStatsResponse>> GetEventTicketStatsAsync(Guid eventId)
        {
            var result = await _ordersService.GetEventTicketStatsAsync(eventId);
            return result;

        }

        /// <summary>
        /// Список билетёров, назначенных на мероприятие
        /// </summary>
        [HttpGet("{eventId}/ticket-staff")]
        public async Task<CommandResult<List<EventTicketStaffResponse>?>> GetEventTicketStaffAsync(Guid eventId)
        {
            var result = await _eventsService.GetEventTicketStaffAsync(eventId);
            return result;

        }

        /// <summary>
        /// Полная замена списка билетёров мероприятия
        /// </summary>
        [HttpPut("{eventId}/ticket-staff")]
        public async Task<CommandResult> SetEventTicketStaffAsync(
            Guid eventId,
            [FromBody] EventTicketStaffUpsertRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                var result = await _eventsService.SetEventTicketStaffAsync(eventId, request);
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
        /// Назначить событию параметры
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="typeids"></param>
        /// <returns></returns>
        [HttpPost("eventParameters/assignToEvent/{eventId}")]
        public async Task<CommandResult> SetEventParametersAsync(Guid eventId, [FromBody] EventParametersRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.SetEventParametersAsync(eventId, request);

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

        
        #region events
        /// <summary>
        /// Создание нового события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateEventAsync(CreateEventRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.CreateEventAsync(request);
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
        /// Назначение событию обложки
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="imageId"></param>
        /// <returns></returns>
        [HttpGet("setCoverImage")]
        public async Task<CommandResult> SetEventCoverImageAsync(Guid eventId, Guid? imageId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.SetEventCoverImageAsync(eventId, imageId);
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
        /// Обновление события
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("update/{id}")]
        public async Task<CommandResult> UpdateEventAsync(Guid id, EventRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.UpdateEventAsync(id, request);
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
        /// получение события по id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get/{id}")]
        public async Task<CommandResult<Event>> GetEventAsync(Guid id)
        {
            var result = await _eventsService.GetEventAsync(id);
            return result;
        }

        /// <summary>
        /// Поиск мероприятий
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("search")]
        public async Task<CommandResult<PagedList<Event>>> SearchEventsAsync(EventsSearchRequest request)
        {
            var result = await _eventsService.SearchEventsAsync(request);
            return result;

        }

        /// <summary>
        /// Поиск мероприятий для отображений на карте
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("search/short")]
        public async Task<CommandResult<PagedList<EventShort>>> SearchEventsShortAsync(EventsSearchRequest request)
        {
            var result = await _eventsService.SearchEventsShortAsync(request);
            return result;

        }

        /// <summary>
        /// Отмена мероприятия
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpDelete("{eventId}/cancel")]
        public async Task<CommandResult> CancelEventAsync(Guid eventId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _eventsService.CancelEventAsync(eventId);

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
