using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.BugReports;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EList.Services.Impl
{
    public class BugReportsService : IBugReportsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.BugReportsService.";
        #endregion

        private readonly IBugReportsRepository _bugReportsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMapper _mapper;

        public BugReportsService(
            IBugReportsRepository bugReportsRepository,
            IAccountDataHolder accountDataHolder,
            ICorrelationIdProvider correlationIdProvider,
            IMapper mapper)
        {
            _bugReportsRepository = bugReportsRepository ?? throw new ArgumentNullException(nameof(bugReportsRepository));
            _accountDataHolder = accountDataHolder;
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<CommandResult<List<BugReportCategory>>> GetCategoriesAsync(bool onlyActive = true)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetCategoriesAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<BugReportCategory>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var result = await _bugReportsRepository.GetCategoriesAsync(onlyActive);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<List<BugReportCategory>>(result);
        }

        public async Task<CommandResult<Guid?>> CreateCategoryAsync(CreateBugReportCategoryRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateCategoryAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            // Пока любой авторизованный пользователь = суперюзер
            if (_accountDataHolder.AccountId == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (string.IsNullOrWhiteSpace(request?.Code) || string.IsNullOrWhiteSpace(request.Name))
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Код и название категории обязательны");

            var code = request.Code.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(code, @"^[a-z0-9_\-]{2,64}$"))
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Код категории: латиница, цифры, _ и - (2-64 символа)");

            var categoryId = await _bugReportsRepository.CreateCategoryAsync(new BugReportCategory
            {
                Code = code,
                Name = request.Name.Trim(),
                SortOrder = request.SortOrder,
                Active = true
            });

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(categoryId);
        }

        public async Task<CommandResult<Guid?>> CreateReportAsync(CreateBugReportRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateReportAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (string.IsNullOrWhiteSpace(request?.Description))
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Описание проблемы обязательно");

            var category = await _bugReportsRepository.GetCategoryByIdAsync(request.CategoryId);
            if (category == null || !category.Active)
                return CommandResult<Guid?>.Fail(ErrorCode.BugReportCategoryNotFound, "Категория не найдена");

            var reportId = await _bugReportsRepository.CreateReportAsync(new BugReport
            {
                ReporterAccountId = _accountDataHolder.AccountId.Value,
                CategoryId = request.CategoryId,
                Description = request.Description.Trim(),
                FileIds = request.FileIds?.Distinct().ToList() ?? new List<Guid>()
            });

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(reportId);
        }

        public async Task<CommandResult<BugReportResponse?>> GetReportAsync(Guid reportId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetReportAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<BugReportResponse?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var report = await _bugReportsRepository.GetReportByIdAsync(reportId);
            if (report == null)
                return CommandResult<BugReportResponse?>.Fail(ErrorCode.BugReportNotFound, $"Багрепорт с id='{reportId}' не найден");

            // Пока суперюзер = любой авторизованный; иначе только свой репорт
            // when superuser system appears: check role here

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<BugReportResponse?>(_mapper.Map<BugReportResponse>(report));
        }

        public async Task<CommandResult<PagedList<BugReportResponse>>> GetMyReportsAsync(int? pageIndex = null, int? pageSize = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyReportsAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<PagedList<BugReportResponse>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var result = await _bugReportsRepository.SearchReportsAsync(new BugReportSearchRequest
            {
                ReporterAccountId = _accountDataHolder.AccountId,
                PageIndex = pageIndex ?? 0,
                PageSize = pageSize ?? 20
            });

            var mapped = new PagedList<BugReportResponse>(
                result.Total,
                result.Result?.Select(i => _mapper.Map<BugReportResponse>(i)).ToList() ?? new List<BugReportResponse>(),
                result.PageIndex,
                result.PageSize);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<BugReportResponse>>(mapped);
        }

        public async Task<CommandResult<PagedList<BugReportResponse>>> SearchReportsAsync(BugReportSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchReportsAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            // Пока любой авторизованный = суперюзер (просмотр всех репортов)
            if (_accountDataHolder.AccountId == null)
                return CommandResult<PagedList<BugReportResponse>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var result = await _bugReportsRepository.SearchReportsAsync(request ?? new BugReportSearchRequest());
            var mapped = new PagedList<BugReportResponse>(
                result.Total,
                result.Result?.Select(i => _mapper.Map<BugReportResponse>(i)).ToList() ?? new List<BugReportResponse>(),
                result.PageIndex,
                result.PageSize);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<BugReportResponse>>(mapped);
        }

        public async Task<CommandResult> SetReportStatusAsync(Guid reportId, UpdateBugReportStatusRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetReportStatusAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            // Пока любой авторизованный = суперюзер
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (!Enum.IsDefined(typeof(BugReportStatus), request.Status))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректный статус");

            var report = await _bugReportsRepository.GetReportByIdAsync(reportId);
            if (report == null)
                return CommandResult.Fail(ErrorCode.BugReportNotFound, $"Багрепорт с id='{reportId}' не найден");

            await _bugReportsRepository.SetReportStatusAsync(reportId, request.Status);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
