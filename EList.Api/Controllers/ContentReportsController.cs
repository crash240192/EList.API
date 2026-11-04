using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.ContentReports;
using EList.Models.Enums;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/contentReports")]
    [LoggerHandlerWebApiFilter]
    public class ContentReportsController : ControllerBase
    {
        private readonly IContentReportsService _contentReportsService;
        private readonly IDataConnectionProvider _connectionProvider;

        public ContentReportsController(
            IContentReportsService contentReportsService, IDataConnectionProvider connectionProvider)
        {
            _contentReportsService = contentReportsService;
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
            return await _contentReportsService.GetReasonsAsync(onlyActive, forTargetType, severity);
        }

        /// <summary>
        /// Получить причину по id
        /// </summary>
        [HttpGet("reasons/{reasonId}")]
        public async Task<CommandResult<ReportReason?>> GetReasonAsync(Guid reasonId)
        {
            return await _contentReportsService.GetReasonAsync(reasonId);
        }

        /// <summary>
        /// Создать причину (admin/superuser)
        /// </summary>
        [HttpPost("reasons/create")]
        public async Task<CommandResult<Guid?>> CreateReasonAsync([FromBody] CreateReportReasonRequest request)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.CreateReasonAsync(request));
        }

        /// <summary>
        /// Обновить причину (admin/superuser)
        /// </summary>
        [HttpPut("reasons/update/{reasonId}")]
        public async Task<CommandResult> UpdateReasonAsync(Guid reasonId, [FromBody] UpdateReportReasonRequest request)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.UpdateReasonAsync(reasonId, request));
        }

        /// <summary>
        /// Активировать / деактивировать причину (admin/superuser)
        /// </summary>
        [HttpPut("reasons/setActive/{reasonId}")]
        public async Task<CommandResult> SetReasonActiveAsync(Guid reasonId, [FromQuery] bool active)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.SetReasonActiveAsync(reasonId, active));
        }

        /// <summary>
        /// Удалить причину без жалоб (admin/superuser)
        /// </summary>
        [HttpDelete("reasons/delete/{reasonId}")]
        public async Task<CommandResult> DeleteReasonAsync(Guid reasonId)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.DeleteReasonAsync(reasonId));
        }

        /// <summary>
        /// Создать жалобу на событие, сообщение, фото, аккаунт, организацию или организатора
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateReportAsync([FromBody] CreateContentReportRequest request)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.CreateReportAsync(request));
        }

        /// <summary>
        /// Получить жалобу по id
        /// </summary>
        [HttpGet("get/{reportId}")]
        public async Task<CommandResult<ContentReportResponse?>> GetReportAsync(Guid reportId)
        {
            return await _contentReportsService.GetReportAsync(reportId);
        }

        /// <summary>
        /// Мои жалобы (я отправитель)
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<PagedList<ContentReportResponse>>> GetMyReportsAsync(
            [FromQuery] int? pageIndex = 0,
            [FromQuery] int? pageSize = 20)
        {
            return await _contentReportsService.GetMyReportsAsync(pageIndex, pageSize);
        }

        /// <summary>
        /// Жалобы и замечания, касающиеся текущего пользователя (без личности жалобщика)
        /// </summary>
        [HttpGet("againstMe")]
        public async Task<CommandResult<PagedList<ContentReportSubjectView>>> GetReportsAgainstMeAsync(
            [FromQuery] int? pageIndex = 0,
            [FromQuery] int? pageSize = 20)
        {
            return await _contentReportsService.GetReportsAgainstMeAsync(pageIndex, pageSize);
        }

        /// <summary>
        /// Карточка жалобы для адресата (без личности жалобщика)
        /// </summary>
        [HttpGet("againstMe/{reportId}")]
        public async Task<CommandResult<ContentReportSubjectView?>> GetReportAgainstMeAsync(Guid reportId)
        {
            return await _contentReportsService.GetReportAgainstMeAsync(reportId);
        }

        /// <summary>
        /// Очередь модерации площадки (moderator/admin/superuser)
        /// </summary>
        [HttpPost("platform/search")]
        public async Task<CommandResult<PagedList<ContentReportResponse>>> SearchPlatformQueueAsync(
            [FromBody] ContentReportsSearchRequest request)
        {
            return await _contentReportsService.SearchPlatformQueueAsync(request);
        }

        /// <summary>
        /// Счётчик очереди площадки
        /// </summary>
        [HttpGet("platform/count")]
        public async Task<CommandResult<int>> CountPlatformQueueAsync([FromQuery] bool onlyActive = true)
        {
            return await _contentReportsService.CountPlatformQueueAsync(onlyActive);
        }

        /// <summary>
        /// Очередь организаторов мероприятия (включая участников организаций-соорганизаторов)
        /// </summary>
        [HttpPost("organizer/{eventId}/search")]
        public async Task<CommandResult<PagedList<ContentReportResponse>>> SearchOrganizerQueueAsync(
            Guid eventId,
            [FromBody] ContentReportsSearchRequest? request)
        {
            return await _contentReportsService.SearchOrganizerQueueAsync(eventId, request);
        }

        /// <summary>
        /// Счётчик очереди организаторов
        /// </summary>
        [HttpGet("organizer/{eventId}/count")]
        public async Task<CommandResult<int>> CountOrganizerQueueAsync(Guid eventId, [FromQuery] bool onlyActive = true)
        {
            return await _contentReportsService.CountOrganizerQueueAsync(eventId, onlyActive);
        }

        /// <summary>
        /// Сводка жалоб и предупреждений по сущности (мероприятие, аккаунт, организация, фото, сообщение, организатор)
        /// </summary>
        [HttpGet("stats/{targetType}/{targetId}")]
        public async Task<CommandResult<ContentReportTargetStats>> GetTargetStatsAsync(
            ReportTargetType targetType,
            Guid targetId)
        {
            return await _contentReportsService.GetTargetStatsAsync(targetType, targetId);
        }

        /// <summary>
        /// Активные модерационные ограничения текущего пользователя
        /// </summary>
        [HttpGet("penalties/my")]
        public async Task<CommandResult<List<ModerationPenalty>>> GetMyPenaltiesAsync()
        {
            return await _contentReportsService.GetMyPenaltiesAsync();
        }

        /// <summary>
        /// Досрочно снять ограничение (moderator/admin/superuser)
        /// </summary>
        [HttpPost("penalties/revoke/{penaltyId}")]
        public async Task<CommandResult> RevokePenaltyAsync(
            Guid penaltyId,
            [FromBody] RevokeModerationPenaltyRequest? request)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.RevokePenaltyAsync(penaltyId, request));
        }

        /// <summary>
        /// Восстановить мероприятие, отменённое модерацией
        /// </summary>
        [HttpPost("restoreEvent/{eventId}")]
        public async Task<CommandResult> RestoreEventAsync(
            Guid eventId,
            [FromBody] RestoreEventRequest? request)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.RestoreEventAsync(eventId, request));
        }

        /// <summary>
        /// Взять жалобу в работу
        /// </summary>
        [HttpPost("take/{reportId}")]
        public async Task<CommandResult> TakeInReviewAsync(Guid reportId)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.TakeInReviewAsync(reportId));
        }

        /// <summary>
        /// Решить жалобу (организатор или модератор площадки)
        /// </summary>
        [HttpPost("resolve/{reportId}")]
        public async Task<CommandResult> ResolveAsync(Guid reportId, [FromBody] ResolveContentReportRequest request)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.ResolveAsync(reportId, request));
        }

        /// <summary>
        /// Эскалировать жалобу на площадку (организатор)
        /// </summary>
        [HttpPost("escalate/{reportId}")]
        public async Task<CommandResult> EscalateAsync(Guid reportId, [FromBody] EscalateContentReportRequest request)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.EscalateAsync(reportId, request));
        }

        /// <summary>
        /// История действий по жалобе
        /// </summary>
        [HttpGet("actions/{reportId}")]
        public async Task<CommandResult<List<ContentReportAction>>> GetActionsAsync(Guid reportId)
        {
            return await _contentReportsService.GetActionsAsync(reportId);
        }

        /// <summary>
        /// Staff proxy: скачать файл жалобы (в т.ч. Blocked) через service-token filestorage.
        /// </summary>
        [HttpGet("get/{reportId}/file")]
        public async Task<IActionResult> DownloadReportedFileAsync(
            Guid reportId,
            [FromHeader(Name = "FullSize")] bool? fullSize)
        {
            var result = await _contentReportsService.DownloadReportedFileAsync(reportId, fullSize);
            if (!result.Success || result.Result == null)
            {
                if (result.ErrorCode == (int)EList.Common.Support.ErrorCode.AccessError)
                    return Unauthorized(result);
                if (result.ErrorCode == (int)EList.Common.Support.ErrorCode.ContentReportNotFound
                    || result.ErrorCode == (int)EList.Common.Support.ErrorCode.AlbumItemNotFound)
                    return NotFound(result);
                return BadRequest(result);
            }
            return File(
                result.Result.Content,
                result.Result.ContentType ?? "application/octet-stream",
                result.Result.FileName);

        }

        /// <summary>
        /// Восстановить Active accessStatus файла жалобы (модератор площадки).
        /// </summary>
        [HttpPost("restoreFileAccess/{reportId}")]
        public async Task<CommandResult> RestoreReportedFileAccessAsync(Guid reportId)
        {
            return await ExecuteTransactionalAsync(() => _contentReportsService.RestoreReportedFileAccessAsync(reportId));
        }

        private async Task<T> ExecuteTransactionalAsync<T>(Func<Task<T>> action)
            where T : CommandResult
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                var result = await action();
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
