using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.ContentReports;
using EList.Models.Enums;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Diagnostics;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/contentReports")]
    [LoggerHandlerWebApiFilter]
    public class ContentReportsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.ContentReportsController.";
        #endregion

        private readonly IContentReportsService _contentReportsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public ContentReportsController(
            IContentReportsService contentReportsService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _contentReportsService = contentReportsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Список причин жалоб
        /// </summary>
        [HttpGet("reasons")]
        public async Task<CommandResult<List<ReportReason>>> GetReasonsAsync(
            [FromQuery] bool onlyActive = true,
            [FromQuery] ReportTargetType? forTargetType = null,
            [FromQuery] ReportSeverity? severity = null)
        {
            return await ExecuteAsync(
                nameof(GetReasonsAsync),
                () => _contentReportsService.GetReasonsAsync(onlyActive, forTargetType, severity));
        }

        /// <summary>
        /// Получить причину по id
        /// </summary>
        [HttpGet("reasons/{reasonId}")]
        public async Task<CommandResult<ReportReason?>> GetReasonAsync(Guid reasonId)
        {
            return await ExecuteAsync(nameof(GetReasonAsync), () => _contentReportsService.GetReasonAsync(reasonId));
        }

        /// <summary>
        /// Создать причину (admin/superuser)
        /// </summary>
        [HttpPost("reasons/create")]
        public async Task<CommandResult<Guid?>> CreateReasonAsync([FromBody] CreateReportReasonRequest request)
        {
            return await ExecuteTransactionalAsync(
                nameof(CreateReasonAsync),
                () => _contentReportsService.CreateReasonAsync(request));
        }

        /// <summary>
        /// Обновить причину (admin/superuser)
        /// </summary>
        [HttpPut("reasons/update/{reasonId}")]
        public async Task<CommandResult> UpdateReasonAsync(Guid reasonId, [FromBody] UpdateReportReasonRequest request)
        {
            return await ExecuteTransactionalAsync(
                nameof(UpdateReasonAsync),
                () => _contentReportsService.UpdateReasonAsync(reasonId, request));
        }

        /// <summary>
        /// Активировать / деактивировать причину (admin/superuser)
        /// </summary>
        [HttpPut("reasons/setActive/{reasonId}")]
        public async Task<CommandResult> SetReasonActiveAsync(Guid reasonId, [FromQuery] bool active)
        {
            return await ExecuteTransactionalAsync(
                nameof(SetReasonActiveAsync),
                () => _contentReportsService.SetReasonActiveAsync(reasonId, active));
        }

        /// <summary>
        /// Удалить причину без жалоб (admin/superuser)
        /// </summary>
        [HttpDelete("reasons/delete/{reasonId}")]
        public async Task<CommandResult> DeleteReasonAsync(Guid reasonId)
        {
            return await ExecuteTransactionalAsync(
                nameof(DeleteReasonAsync),
                () => _contentReportsService.DeleteReasonAsync(reasonId));
        }

        /// <summary>
        /// Создать жалобу на событие, сообщение, фото, аккаунт, организацию или организатора
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateReportAsync([FromBody] CreateContentReportRequest request)
        {
            return await ExecuteTransactionalAsync(
                nameof(CreateReportAsync),
                () => _contentReportsService.CreateReportAsync(request));
        }

        /// <summary>
        /// Получить жалобу по id
        /// </summary>
        [HttpGet("get/{reportId}")]
        public async Task<CommandResult<ContentReportResponse?>> GetReportAsync(Guid reportId)
        {
            return await ExecuteAsync(nameof(GetReportAsync), () => _contentReportsService.GetReportAsync(reportId));
        }

        /// <summary>
        /// Мои жалобы (я отправитель)
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<PagedList<ContentReportResponse>>> GetMyReportsAsync(
            [FromQuery] int? pageIndex = 0,
            [FromQuery] int? pageSize = 20)
        {
            return await ExecuteAsync(
                nameof(GetMyReportsAsync),
                () => _contentReportsService.GetMyReportsAsync(pageIndex, pageSize));
        }

        /// <summary>
        /// Жалобы и замечания, касающиеся текущего пользователя (без личности жалобщика)
        /// </summary>
        [HttpGet("againstMe")]
        public async Task<CommandResult<PagedList<ContentReportSubjectView>>> GetReportsAgainstMeAsync(
            [FromQuery] int? pageIndex = 0,
            [FromQuery] int? pageSize = 20)
        {
            return await ExecuteAsync(
                nameof(GetReportsAgainstMeAsync),
                () => _contentReportsService.GetReportsAgainstMeAsync(pageIndex, pageSize));
        }

        /// <summary>
        /// Карточка жалобы для адресата (без личности жалобщика)
        /// </summary>
        [HttpGet("againstMe/{reportId}")]
        public async Task<CommandResult<ContentReportSubjectView?>> GetReportAgainstMeAsync(Guid reportId)
        {
            return await ExecuteAsync(
                nameof(GetReportAgainstMeAsync),
                () => _contentReportsService.GetReportAgainstMeAsync(reportId));
        }

        /// <summary>
        /// Очередь модерации площадки (moderator/admin/superuser)
        /// </summary>
        [HttpPost("platform/search")]
        public async Task<CommandResult<PagedList<ContentReportResponse>>> SearchPlatformQueueAsync(
            [FromBody] ContentReportsSearchRequest request)
        {
            return await ExecuteAsync(
                nameof(SearchPlatformQueueAsync),
                () => _contentReportsService.SearchPlatformQueueAsync(request));
        }

        /// <summary>
        /// Счётчик очереди площадки
        /// </summary>
        [HttpGet("platform/count")]
        public async Task<CommandResult<int>> CountPlatformQueueAsync([FromQuery] bool onlyActive = true)
        {
            return await ExecuteAsync(
                nameof(CountPlatformQueueAsync),
                () => _contentReportsService.CountPlatformQueueAsync(onlyActive));
        }

        /// <summary>
        /// Очередь организаторов мероприятия (включая участников организаций-соорганизаторов)
        /// </summary>
        [HttpPost("organizer/{eventId}/search")]
        public async Task<CommandResult<PagedList<ContentReportResponse>>> SearchOrganizerQueueAsync(
            Guid eventId,
            [FromBody] ContentReportsSearchRequest? request)
        {
            return await ExecuteAsync(
                nameof(SearchOrganizerQueueAsync),
                () => _contentReportsService.SearchOrganizerQueueAsync(eventId, request));
        }

        /// <summary>
        /// Счётчик очереди организаторов
        /// </summary>
        [HttpGet("organizer/{eventId}/count")]
        public async Task<CommandResult<int>> CountOrganizerQueueAsync(Guid eventId, [FromQuery] bool onlyActive = true)
        {
            return await ExecuteAsync(
                nameof(CountOrganizerQueueAsync),
                () => _contentReportsService.CountOrganizerQueueAsync(eventId, onlyActive));
        }

        /// <summary>
        /// Взять жалобу в работу
        /// </summary>
        [HttpPost("take/{reportId}")]
        public async Task<CommandResult> TakeInReviewAsync(Guid reportId)
        {
            return await ExecuteTransactionalAsync(
                nameof(TakeInReviewAsync),
                () => _contentReportsService.TakeInReviewAsync(reportId));
        }

        /// <summary>
        /// Решить жалобу (организатор или модератор площадки)
        /// </summary>
        [HttpPost("resolve/{reportId}")]
        public async Task<CommandResult> ResolveAsync(Guid reportId, [FromBody] ResolveContentReportRequest request)
        {
            return await ExecuteTransactionalAsync(
                nameof(ResolveAsync),
                () => _contentReportsService.ResolveAsync(reportId, request));
        }

        /// <summary>
        /// Эскалировать жалобу на площадку (организатор)
        /// </summary>
        [HttpPost("escalate/{reportId}")]
        public async Task<CommandResult> EscalateAsync(Guid reportId, [FromBody] EscalateContentReportRequest request)
        {
            return await ExecuteTransactionalAsync(
                nameof(EscalateAsync),
                () => _contentReportsService.EscalateAsync(reportId, request));
        }

        /// <summary>
        /// История действий по жалобе
        /// </summary>
        [HttpGet("actions/{reportId}")]
        public async Task<CommandResult<List<ContentReportAction>>> GetActionsAsync(Guid reportId)
        {
            return await ExecuteAsync(nameof(GetActionsAsync), () => _contentReportsService.GetActionsAsync(reportId));
        }

        private async Task<T> ExecuteAsync<T>(string methodShortName, Func<Task<T>> action)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{methodShortName}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await action();
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        private async Task<T> ExecuteTransactionalAsync<T>(string methodShortName, Func<Task<T>> action)
            where T : CommandResult
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{methodShortName}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await action();
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
    }
}
