using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.BugReports;
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
    [Route("/api/bugReports")]
    [LoggerHandlerWebApiFilter]
    public class BugReportsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.BugReportsController.";
        #endregion

        private readonly IBugReportsService _bugReportsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public BugReportsController(
            IBugReportsService bugReportsService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _bugReportsService = bugReportsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Список категорий багрепортов (разделы сайта)
        /// </summary>
        [HttpGet("categories")]
        public async Task<CommandResult<List<BugReportCategory>>> GetCategoriesAsync([FromQuery] bool onlyActive = true)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetCategoriesAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _bugReportsService.GetCategoriesAsync(onlyActive);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Создать категорию (админ; пока любой авторизованный)
        /// </summary>
        [HttpPost("categories/create")]
        public async Task<CommandResult<Guid?>> CreateCategoryAsync([FromBody] CreateBugReportCategoryRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateCategoryAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _bugReportsService.CreateCategoryAsync(request);
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

        /// <summary>
        /// Создать багрепорт
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateReportAsync([FromBody] CreateBugReportRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateReportAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _bugReportsService.CreateReportAsync(request);
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

        /// <summary>
        /// Получить багрепорт по id
        /// </summary>
        [HttpGet("get/{reportId}")]
        public async Task<CommandResult<BugReportResponse?>> GetReportAsync(Guid reportId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetReportAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _bugReportsService.GetReportAsync(reportId);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Мои багрепорты
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<PagedList<BugReportResponse>>> GetMyReportsAsync([FromQuery] int? pageIndex = 0, [FromQuery] int? pageSize = 20)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyReportsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _bugReportsService.GetMyReportsAsync(pageIndex, pageSize);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Поиск багрепортов (админ; пока любой авторизованный)
        /// </summary>
        [HttpPost("search")]
        public async Task<CommandResult<PagedList<BugReportResponse>>> SearchReportsAsync([FromBody] BugReportSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchReportsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _bugReportsService.SearchReportsAsync(request);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Обновить статус багрепорта (админ; пока любой авторизованный)
        /// </summary>
        [HttpPut("status/{reportId}")]
        public async Task<CommandResult> SetReportStatusAsync(Guid reportId, [FromBody] UpdateBugReportStatusRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetReportStatusAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _bugReportsService.SetReportStatusAsync(reportId, request);
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
