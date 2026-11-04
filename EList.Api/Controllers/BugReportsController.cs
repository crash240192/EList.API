using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.BugReports;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/bugReports")]
    [LoggerHandlerWebApiFilter]
    public class BugReportsController : ControllerBase
    {
        private readonly IBugReportsService _bugReportsService;
        private readonly IDataConnectionProvider _connectionProvider;

        public BugReportsController(
            IBugReportsService bugReportsService, IDataConnectionProvider connectionProvider)
        {
            _bugReportsService = bugReportsService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Список категорий багрепортов (разделы сайта)
        /// </summary>
        [HttpGet("categories")]
        public async Task<CommandResult<List<BugReportCategory>>> GetCategoriesAsync([FromQuery] bool onlyActive = true)
        {
            var result = await _bugReportsService.GetCategoriesAsync(onlyActive);
            return result;

        }

        /// <summary>
        /// Получить категорию по id
        /// </summary>
        [HttpGet("categories/get/{categoryId}")]
        public async Task<CommandResult<BugReportCategory?>> GetCategoryAsync(Guid categoryId)
        {
            var result = await _bugReportsService.GetCategoryAsync(categoryId);
            return result;

        }

        /// <summary>
        /// Создать категорию (админ; пока любой авторизованный)
        /// </summary>
        [HttpPost("categories/create")]
        public async Task<CommandResult<Guid?>> CreateCategoryAsync([FromBody] CreateBugReportCategoryRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _bugReportsService.CreateCategoryAsync(request);
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
        /// Обновить категорию (админ; пока любой авторизованный)
        /// </summary>
        [HttpPut("categories/update/{categoryId}")]
        public async Task<CommandResult> UpdateCategoryAsync(Guid categoryId, [FromBody] UpdateBugReportCategoryRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _bugReportsService.UpdateCategoryAsync(categoryId, request);
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
        /// Включить / выключить категорию (админ; пока любой авторизованный)
        /// </summary>
        [HttpPut("categories/setActive/{categoryId}")]
        public async Task<CommandResult> SetCategoryActiveAsync(Guid categoryId, [FromQuery] bool active)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _bugReportsService.SetCategoryActiveAsync(categoryId, active);
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
        /// Удалить категорию без репортов (админ; пока любой авторизованный)
        /// </summary>
        [HttpDelete("categories/delete/{categoryId}")]
        public async Task<CommandResult> DeleteCategoryAsync(Guid categoryId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _bugReportsService.DeleteCategoryAsync(categoryId);
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
        /// Создать багрепорт
        /// </summary>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateReportAsync([FromBody] CreateBugReportRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _bugReportsService.CreateReportAsync(request);
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
        /// Получить багрепорт по id
        /// </summary>
        [HttpGet("get/{reportId}")]
        public async Task<CommandResult<BugReportResponse?>> GetReportAsync(Guid reportId)
        {
            var result = await _bugReportsService.GetReportAsync(reportId);
            return result;
        }

        /// <summary>
        /// Мои багрепорты
        /// </summary>
        [HttpGet("my")]
        public async Task<CommandResult<PagedList<BugReportResponse>>> GetMyReportsAsync([FromQuery] int? pageIndex = 0, [FromQuery] int? pageSize = 20)
        {
            var result = await _bugReportsService.GetMyReportsAsync(pageIndex, pageSize);
            return result;

        }

        /// <summary>
        /// Поиск багрепортов (админ; пока любой авторизованный)
        /// </summary>
        [HttpPost("search")]
        public async Task<CommandResult<PagedList<BugReportResponse>>> SearchReportsAsync([FromBody] BugReportSearchRequest request)
        {
            var result = await _bugReportsService.SearchReportsAsync(request);
            return result;

        }

        /// <summary>
        /// Обновить статус багрепорта (админ; пока любой авторизованный)
        /// </summary>
        [HttpPut("status/{reportId}")]
        public async Task<CommandResult> SetReportStatusAsync(Guid reportId, [FromBody] UpdateBugReportStatusRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _bugReportsService.SetReportStatusAsync(reportId, request);
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
