using AutoMapper;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.ContentReports;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EList.Services.Impl
{
    public class ContentReportsService : IContentReportsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.ContentReportsService.";
        #endregion

        private readonly IContentReportsRepository _contentReportsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IEventsRepository _eventsRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IMediaRepository _mediaRepository;
        private readonly INotificationsService _notificationsService;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMapper _mapper;

        public ContentReportsService(
            IContentReportsRepository contentReportsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IEventsRepository eventsRepository,
            IConversationRepository conversationRepository,
            IParticipantsBWListRepository participantsBWListRepository,
            IAccountsRepository accountsRepository,
            IOrganizationsRepository organizationsRepository,
            IMediaRepository mediaRepository,
            INotificationsService notificationsService,
            IAccountDataHolder accountDataHolder,
            ICorrelationIdProvider correlationIdProvider,
            IMapper mapper)
        {
            _contentReportsRepository = contentReportsRepository ?? throw new ArgumentNullException(nameof(contentReportsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new ArgumentNullException(nameof(participantsBWListRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _accountDataHolder = accountDataHolder;
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<CommandResult<List<ReportReason>>> GetReasonsAsync(
            bool onlyActive = true,
            ReportTargetType? forTargetType = null,
            ReportSeverity? severity = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetReasonsAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<ReportReason>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var result = await _contentReportsRepository.GetReasonsAsync(onlyActive, forTargetType, severity);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<List<ReportReason>>(result);
        }

        public async Task<CommandResult<ReportReason?>> GetReasonAsync(Guid reasonId)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<ReportReason?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var reason = await _contentReportsRepository.GetReasonByIdAsync(reasonId);
            if (reason == null)
                return CommandResult<ReportReason?>.Fail(ErrorCode.ReportReasonNotFound, "Причина жалобы не найдена");

            return new CommandResult<ReportReason?>(reason);
        }

        public async Task<CommandResult<Guid?>> CreateReasonAsync(CreateReportReasonRequest request)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            if (string.IsNullOrWhiteSpace(request?.Code) || string.IsNullOrWhiteSpace(request.Name))
                return CommandResult<Guid?>.Fail(ErrorCode.IsNullOrEmpty, "Код и название обязательны");

            var code = request.Code.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(code, @"^[a-z0-9_\-]{2,64}$"))
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Код: латиница, цифры, _ и - (2-64)");

            if (await _contentReportsRepository.ReasonCodeExistsAsync(code))
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Причина с таким кодом уже существует");

            var id = await _contentReportsRepository.CreateReasonAsync(new ReportReason
            {
                Code = code,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                TargetScope = request.TargetScope,
                Severity = request.Severity,
                PrimaryQueue = request.PrimaryQueue,
                SortOrder = request.SortOrder,
                Active = true
            });

            return new CommandResult<Guid?>(id);
        }

        public async Task<CommandResult> UpdateReasonAsync(Guid reasonId, UpdateReportReasonRequest request)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var reason = await _contentReportsRepository.GetReasonByIdAsync(reasonId);
            if (reason == null)
                return CommandResult.Fail(ErrorCode.ReportReasonNotFound, "Причина жалобы не найдена");

            if (!string.IsNullOrWhiteSpace(request?.Code))
            {
                var code = request.Code.Trim().ToLowerInvariant();
                if (!Regex.IsMatch(code, @"^[a-z0-9_\-]{2,64}$"))
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Код: латиница, цифры, _ и - (2-64)");
                if (await _contentReportsRepository.ReasonCodeExistsAsync(code, reasonId))
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Причина с таким кодом уже существует");
                reason.Code = code;
            }

            if (!string.IsNullOrWhiteSpace(request?.Name))
                reason.Name = request.Name.Trim();
            if (request?.Description != null)
                reason.Description = request.Description.Trim();
            if (request?.TargetScope != null)
                reason.TargetScope = request.TargetScope.Value;
            if (request?.Severity != null)
                reason.Severity = request.Severity.Value;
            if (request?.PrimaryQueue != null)
                reason.PrimaryQueue = request.PrimaryQueue.Value;
            if (request?.SortOrder != null)
                reason.SortOrder = request.SortOrder.Value;
            if (request?.Active != null)
                reason.Active = request.Active.Value;

            await _contentReportsRepository.UpdateReasonAsync(reason);
            return CommandResult.OK;
        }

        public async Task<CommandResult> SetReasonActiveAsync(Guid reasonId, bool active)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var reason = await _contentReportsRepository.GetReasonByIdAsync(reasonId);
            if (reason == null)
                return CommandResult.Fail(ErrorCode.ReportReasonNotFound, "Причина жалобы не найдена");

            await _contentReportsRepository.SetReasonActiveAsync(reasonId, active);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteReasonAsync(Guid reasonId)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var reason = await _contentReportsRepository.GetReasonByIdAsync(reasonId);
            if (reason == null)
                return CommandResult.Fail(ErrorCode.ReportReasonNotFound, "Причина жалобы не найдена");

            if (await _contentReportsRepository.CountReportsByReasonAsync(reasonId) > 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя удалить причину с жалобами — деактивируйте её");

            await _contentReportsRepository.DeleteReasonAsync(reasonId);
            return CommandResult.OK;
        }

        public async Task<CommandResult<Guid?>> CreateReportAsync(CreateContentReportRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateReportAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (!Enum.IsDefined(typeof(ReportTargetType), request.TargetType))
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Некорректный тип цели");

            var reason = await _contentReportsRepository.GetReasonByIdAsync(request.ReasonId);
            if (reason == null || !reason.Active)
                return CommandResult<Guid?>.Fail(ErrorCode.ReportReasonNotFound, "Причина жалобы не найдена");

            if (!IsReasonApplicable(reason, request.TargetType))
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Причина не применима к указанному типу контента");

            var existing = await _contentReportsRepository.GetOpenReportByReporterAndTargetAsync(
                _accountDataHolder.AccountId.Value, request.TargetType, request.TargetId);
            if (existing != null)
                return CommandResult<Guid?>.Fail(ErrorCode.ContentReportAlreadyExists, "У вас уже есть активная жалоба на этот объект");

            var report = new ContentReport
            {
                ReporterAccountId = _accountDataHolder.AccountId.Value,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                ReasonId = request.ReasonId,
                Comment = request.Comment?.Trim(),
                AlbumId = request.AlbumId
            };

            var fillResult = await FillReportTargetAsync(report, request);
            if (!fillResult.Success)
                return CommandResult<Guid?>.Fail(fillResult.ErrorCode, fillResult.Message);

            _contentReportsRepository.ApplyDefaultQueueStatuses(report, reason);
            var reportId = await _contentReportsRepository.CreateReportAsync(report);
            report.Id = reportId;
            report.Reason = reason;

            await _contentReportsRepository.AddActionAsync(new ContentReportAction
            {
                ReportId = reportId,
                ActorAccountId = _accountDataHolder.AccountId,
                ActorContext = ReportActorContext.Reporter,
                Action = "created",
                Details = JsonSerializer.Serialize(new { reasonCode = reason.Code, targetType = request.TargetType.ToString() })
            });

            await _notificationsService.NotifyContentReportCreatedAsync(report);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(reportId);
        }

        public async Task<CommandResult<ContentReportResponse?>> GetReportAsync(Guid reportId)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<ContentReportResponse?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var report = await _contentReportsRepository.GetReportByIdAsync(reportId, includeActions: true);
            if (report == null)
                return CommandResult<ContentReportResponse?>.Fail(ErrorCode.ContentReportNotFound, "Жалоба не найдена");

            if (!await CanViewReportAsync(report))
                return CommandResult<ContentReportResponse?>.Fail(ErrorCode.AccessError, "Недостаточно прав для просмотра жалобы");

            return new CommandResult<ContentReportResponse?>(_mapper.Map<ContentReportResponse>(report));
        }

        public async Task<CommandResult<PagedList<ContentReportResponse>>> GetMyReportsAsync(int? pageIndex = null, int? pageSize = null)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<PagedList<ContentReportResponse>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var result = await _contentReportsRepository.SearchReportsAsync(new ContentReportsSearchRequest
            {
                ReporterAccountId = _accountDataHolder.AccountId,
                PageIndex = pageIndex ?? 0,
                PageSize = pageSize ?? 20
            });

            return new CommandResult<PagedList<ContentReportResponse>>(MapPaged(result));
        }

        public async Task<CommandResult<PagedList<ContentReportResponse>>> SearchPlatformQueueAsync(ContentReportsSearchRequest request)
        {
            if (!_accountDataHolder.IsPlatformModeratorOrAbove)
                return CommandResult<PagedList<ContentReportResponse>>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            request ??= new ContentReportsSearchRequest();
            request.InPlatformQueue = true;
            request.PageIndex ??= 0;
            request.PageSize ??= 20;

            var result = await _contentReportsRepository.SearchReportsAsync(request);
            return new CommandResult<PagedList<ContentReportResponse>>(MapPaged(result));
        }

        public async Task<CommandResult<PagedList<ContentReportResponse>>> SearchOrganizerQueueAsync(
            Guid eventId,
            ContentReportsSearchRequest? request = null)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<PagedList<ContentReportResponse>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var ev = await _eventsRepository.GetEventAsync(eventId);
            if (ev == null)
                return CommandResult<PagedList<ContentReportResponse>>.Fail(ErrorCode.EventNotFound, "Событие не найдено");

            if (!await IsEventOrganizerAsync(eventId) && !_accountDataHolder.IsPlatformModeratorOrAbove)
                return CommandResult<PagedList<ContentReportResponse>>.Fail(ErrorCode.AccessError, "Недостаточно прав: нужен организатор мероприятия или модератор площадки");

            request ??= new ContentReportsSearchRequest();
            request.EventId = eventId;
            request.InOrganizerQueue = true;
            request.PageIndex ??= 0;
            request.PageSize ??= 20;

            var result = await _contentReportsRepository.SearchReportsAsync(request);
            return new CommandResult<PagedList<ContentReportResponse>>(MapPaged(result));
        }

        public async Task<CommandResult<int>> CountPlatformQueueAsync(bool onlyActive = true)
        {
            if (!_accountDataHolder.IsPlatformModeratorOrAbove)
                return CommandResult<int>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var count = await _contentReportsRepository.CountReportsAsync(new ContentReportsSearchRequest
            {
                InPlatformQueue = true,
                OnlyActive = onlyActive
            });
            return new CommandResult<int>(count);
        }

        public async Task<CommandResult<int>> CountOrganizerQueueAsync(Guid eventId, bool onlyActive = true)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<int>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (!await IsEventOrganizerAsync(eventId) && !_accountDataHolder.IsPlatformModeratorOrAbove)
                return CommandResult<int>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var count = await _contentReportsRepository.CountReportsAsync(new ContentReportsSearchRequest
            {
                EventId = eventId,
                InOrganizerQueue = true,
                OnlyActive = onlyActive
            });
            return new CommandResult<int>(count);
        }

        public async Task<CommandResult> TakeInReviewAsync(Guid reportId)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var report = await _contentReportsRepository.GetReportByIdAsync(reportId);
            if (report == null)
                return CommandResult.Fail(ErrorCode.ContentReportNotFound, "Жалоба не найдена");

            var asPlatform = _accountDataHolder.IsPlatformModeratorOrAbove && report.PlatformStatus != null;
            var asOrganizer = report.OrganizerStatus != null
                && report.EventId != null
                && await IsEventOrganizerAsync(report.EventId.Value);

            if (!asPlatform && !asOrganizer)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав для обработки жалобы");

            if (await IsReportSubjectAsync(report))
                return CommandResult.Fail(ErrorCode.AccessError, "Нельзя модерировать жалобу, предметом которой вы являетесь");

            await _contentReportsRepository.AssignReportAsync(reportId, _accountDataHolder.AccountId);

            if (asOrganizer)
                await _contentReportsRepository.SetOrganizerStatusAsync(reportId, ReportStatus.InReview);
            if (asPlatform)
                await _contentReportsRepository.SetPlatformStatusAsync(reportId, ReportStatus.InReview);

            await _contentReportsRepository.AddActionAsync(new ContentReportAction
            {
                ReportId = reportId,
                ActorAccountId = _accountDataHolder.AccountId,
                ActorContext = asPlatform ? ReportActorContext.PlatformModerator : ReportActorContext.Organizer,
                Action = "taken_in_review"
            });

            return CommandResult.OK;
        }

        public async Task<CommandResult> ResolveAsync(Guid reportId, ResolveContentReportRequest request)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (!Enum.IsDefined(typeof(ReportResolutionAction), request.ResolutionAction))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректное действие");

            if (request.ResolutionAction == ReportResolutionAction.Escalate)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Для эскалации используйте отдельный метод escalate");

            var report = await _contentReportsRepository.GetReportByIdAsync(reportId);
            if (report == null)
                return CommandResult.Fail(ErrorCode.ContentReportNotFound, "Жалоба не найдена");

            var asPlatform = _accountDataHolder.IsPlatformModeratorOrAbove && report.PlatformStatus != null;
            var asOrganizer = report.OrganizerStatus != null
                && report.EventId != null
                && await IsEventOrganizerAsync(report.EventId.Value);

            if (!asPlatform && !asOrganizer)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав для обработки жалобы");

            if (await IsReportSubjectAsync(report))
                return CommandResult.Fail(ErrorCode.AccessError, "Нельзя модерировать жалобу, предметом которой вы являетесь");

            if (!asPlatform && IsPlatformOnlyResolution(request.ResolutionAction))
                return CommandResult.Fail(ErrorCode.AccessError, "Это действие доступно только модератору площадки");

            var applyResult = await ApplyResolutionActionAsync(report, request, asPlatform);
            if (!applyResult.Success)
                return applyResult;

            var finalStatus = request.ResolutionAction == ReportResolutionAction.Dismiss
                ? ReportStatus.Dismissed
                : ReportStatus.Resolved;

            ReportStatus? organizerStatus = asOrganizer ? finalStatus : null;
            ReportStatus? platformStatus = asPlatform ? finalStatus : null;

            // Если safety-кейс закрывает платформа — закрываем и очередь организаторов.
            if (asPlatform && report.OrganizerStatus != null)
                organizerStatus = finalStatus;

            // Если community-кейс закрывает организатор, а platform queue не заведена — общий статус финальный.
            // Если platform queue есть (safety) и закрыл только организатор — общий статус остаётся open/escalated для платформы.
            var overallStatus = finalStatus;
            if (asOrganizer && !asPlatform && report.PlatformStatus != null
                && report.PlatformStatus != ReportStatus.Resolved
                && report.PlatformStatus != ReportStatus.Dismissed)
            {
                overallStatus = ReportStatus.Open;
            }

            await _contentReportsRepository.ResolveReportAsync(
                reportId,
                overallStatus,
                request.ResolutionAction,
                request.ResolutionComment?.Trim(),
                _accountDataHolder.AccountId.Value,
                organizerStatus,
                platformStatus);

            await _contentReportsRepository.AddActionAsync(new ContentReportAction
            {
                ReportId = reportId,
                ActorAccountId = _accountDataHolder.AccountId,
                ActorContext = asPlatform ? ReportActorContext.PlatformModerator : ReportActorContext.Organizer,
                Action = "resolved",
                Details = JsonSerializer.Serialize(new
                {
                    action = request.ResolutionAction.ToString(),
                    comment = request.ResolutionComment
                })
            });

            await _notificationsService.NotifyContentReportResolvedAsync(
                report,
                request.ResolutionAction,
                request.ResolutionComment);

            return CommandResult.OK;
        }

        public async Task<CommandResult> EscalateAsync(Guid reportId, EscalateContentReportRequest request)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var report = await _contentReportsRepository.GetReportByIdAsync(reportId);
            if (report == null)
                return CommandResult.Fail(ErrorCode.ContentReportNotFound, "Жалоба не найдена");

            if (report.EventId == null || !await IsEventOrganizerAsync(report.EventId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, "Эскалировать может только организатор мероприятия");

            if (report.OrganizerStatus == null)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Жалоба не находится в очереди организаторов");

            await _contentReportsRepository.EscalateToPlatformAsync(
                reportId,
                _accountDataHolder.AccountId,
                request?.Comment);

            await _notificationsService.NotifyContentReportEscalatedAsync(report);

            return CommandResult.OK;
        }

        public async Task<CommandResult<List<ContentReportAction>>> GetActionsAsync(Guid reportId)
        {
            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<ContentReportAction>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var report = await _contentReportsRepository.GetReportByIdAsync(reportId);
            if (report == null)
                return CommandResult<List<ContentReportAction>>.Fail(ErrorCode.ContentReportNotFound, "Жалоба не найдена");

            if (!await CanViewReportAsync(report))
                return CommandResult<List<ContentReportAction>>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var actions = await _contentReportsRepository.GetActionsByReportIdAsync(reportId);
            return new CommandResult<List<ContentReportAction>>(actions);
        }

        private async Task<CommandResult> ApplyResolutionActionAsync(
            ContentReport report,
            ResolveContentReportRequest request,
            bool asPlatform)
        {
            switch (request.ResolutionAction)
            {
                case ReportResolutionAction.Dismiss:
                case ReportResolutionAction.Warn:
                case ReportResolutionAction.Other:
                    return CommandResult.OK;

                case ReportResolutionAction.HideContent:
                    if (report.TargetType == ReportTargetType.Message && report.MessageId != null)
                    {
                        await _contentReportsRepository.SetMessageHiddenAsync(
                            report.MessageId.Value, true, _accountDataHolder.AccountId);
                        return CommandResult.OK;
                    }
                    if (report.TargetType == ReportTargetType.Photo)
                        return await ApplyPhotoModerationAsync(report, delete: false);
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Скрытие поддерживается для сообщений и фото");

                case ReportResolutionAction.DeleteContent:
                    if (report.TargetType == ReportTargetType.Message && report.MessageId != null)
                    {
                        await _contentReportsRepository.SetMessageHiddenAsync(
                            report.MessageId.Value, true, _accountDataHolder.AccountId);
                        await _conversationRepository.DeleteMessageAsync(report.MessageId.Value);
                        return CommandResult.OK;
                    }
                    if (report.TargetType == ReportTargetType.Photo)
                        return await ApplyPhotoModerationAsync(report, delete: true);
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Удаление контента поддерживается для сообщений и фото");

                case ReportResolutionAction.BanFromEvent:
                    if (report.EventId == null)
                        return CommandResult.Fail(ErrorCode.InvalidValue, "Не указано мероприятие для бана");

                    var accountId = request.TargetAccountId
                        ?? report.ReportedAccountId
                        ?? TryGetAccountIdFromSnapshot(report.TargetSnapshot)
                        ?? report.Message?.AccountId;

                    if (accountId == null)
                        return CommandResult.Fail(ErrorCode.InvalidValue, "Не удалось определить аккаунт для бана");

                    await _participantsBWListRepository.AddToBlackListAsync(
                        new Models.Participation.AddUsersToBWListRequest
                        {
                            EventId = report.EventId.Value,
                            AccountIds = new List<Guid> { accountId.Value }
                        });
                    await _notificationsService.NotifyAddedToBlackListAsync(
                        report.EventId.Value,
                        new List<Guid> { accountId.Value });
                    return CommandResult.OK;

                case ReportResolutionAction.CancelEvent:
                    if (!asPlatform)
                        return CommandResult.Fail(ErrorCode.AccessError, "Отменить мероприятие может только модератор площадки");
                    if (report.EventId == null)
                        return CommandResult.Fail(ErrorCode.EventNotFound, "Мероприятие не найдено");

                    await _eventsRepository.CancelEventAsync(report.EventId.Value);
                    await _notificationsService.NotifyEventCancelledAsync(report.EventId.Value);
                    return CommandResult.OK;

                case ReportResolutionAction.SuspendAccount:
                    if (!asPlatform)
                        return CommandResult.Fail(ErrorCode.AccessError, "Блокировать аккаунт может только модератор площадки");

                    var suspendAccountId = request.TargetAccountId
                        ?? report.ReportedAccountId
                        ?? (report.TargetType == ReportTargetType.Account ? report.TargetId : (Guid?)null)
                        ?? TryGetAccountIdFromSnapshot(report.TargetSnapshot);

                    if (suspendAccountId == null)
                        return CommandResult.Fail(ErrorCode.InvalidValue, "Не удалось определить аккаунт для блокировки");

                    var account = await _accountsRepository.GetAccountAsync(suspendAccountId.Value);
                    if (account == null)
                        return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

                    await _accountsRepository.SetAccountActiveAsync(suspendAccountId.Value, false);
                    return CommandResult.OK;

                case ReportResolutionAction.SuspendOrganization:
                    if (!asPlatform)
                        return CommandResult.Fail(ErrorCode.AccessError, "Приостановить организацию может только модератор площадки");

                    var suspendOrgId = report.OrganizationId
                        ?? (report.TargetType == ReportTargetType.Organization ? report.TargetId : (Guid?)null);

                    if (suspendOrgId == null)
                        return CommandResult.Fail(ErrorCode.InvalidValue, "Не удалось определить организацию");

                    var organization = await _organizationsRepository.GetOrganizationAsync(suspendOrgId.Value);
                    if (organization == null)
                        return CommandResult.Fail(ErrorCode.OrganizationNotFound, "Организация не найдена");

                    await _organizationsRepository.SetOrganizationActiveAsync(suspendOrgId.Value, false);
                    return CommandResult.OK;

                case ReportResolutionAction.RemoveOrganizator:
                    if (!asPlatform)
                        return CommandResult.Fail(ErrorCode.AccessError, "Снять организатора может только модератор площадки");

                    var organizatorId = report.EventOrganizatorId
                        ?? (report.TargetType == ReportTargetType.EventOrganizator ? report.TargetId : (Guid?)null);

                    if (organizatorId == null)
                        return CommandResult.Fail(ErrorCode.InvalidValue, "Не указан организатор для снятия");

                    var organizator = await _eventOrganizatorsRepository.GetByIdAsync(organizatorId.Value);
                    if (organizator == null)
                        return CommandResult.Fail(ErrorCode.InvalidValue, "Запись организатора не найдена");

                    await _eventOrganizatorsRepository.DeleteAsync(organizatorId.Value);
                    return CommandResult.OK;

                case ReportResolutionAction.ResetAvatar:
                    return await ResetAvatarAsync(report);

                default:
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректное действие");
            }
        }

        private async Task<bool> CanViewReportAsync(ContentReport report)
        {
            if (_accountDataHolder.AccountId == null)
                return false;

            if (report.ReporterAccountId == _accountDataHolder.AccountId)
                return true;

            if (_accountDataHolder.IsPlatformModeratorOrAbove)
                return true;

            return report.OrganizerStatus != null
                && report.EventId != null
                && await IsEventOrganizerAsync(report.EventId.Value);
        }

        private async Task<bool> IsEventOrganizerAsync(Guid eventId)
        {
            if (_accountDataHolder.AccountId == null)
                return false;

            // Учитывает прямых организаторов и активных участников организаций-соорганизаторов
            return await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(
                eventId,
                _accountDataHolder.AccountId.Value);
        }

        private static bool IsReasonApplicable(ReportReason reason, ReportTargetType targetType)
        {
            if (reason.TargetScope == ReportTargetScope.All)
                return true;

            if (reason.TargetScope == ReportTargetScope.Both)
                return targetType == ReportTargetType.Event || targetType == ReportTargetType.Message;

            return (targetType == ReportTargetType.Event && reason.TargetScope == ReportTargetScope.Event)
                || (targetType == ReportTargetType.Message && reason.TargetScope == ReportTargetScope.Message)
                || (targetType == ReportTargetType.Photo && reason.TargetScope == ReportTargetScope.Photo)
                || (targetType == ReportTargetType.Account && reason.TargetScope == ReportTargetScope.Account)
                || (targetType == ReportTargetType.Organization && reason.TargetScope == ReportTargetScope.Organization)
                || (targetType == ReportTargetType.EventOrganizator && reason.TargetScope == ReportTargetScope.EventOrganizator);
        }

        private static bool IsPlatformOnlyResolution(ReportResolutionAction action)
        {
            return action is ReportResolutionAction.CancelEvent
                or ReportResolutionAction.SuspendAccount
                or ReportResolutionAction.SuspendOrganization
                or ReportResolutionAction.RemoveOrganizator;
        }

        private async Task<CommandResult> FillReportTargetAsync(ContentReport report, CreateContentReportRequest request)
        {
            switch (request.TargetType)
            {
                case ReportTargetType.Event:
                    {
                        var ev = await _eventsRepository.GetEventAsync(request.TargetId);
                        if (ev == null)
                            return CommandResult.Fail(ErrorCode.EventNotFound, "Событие не найдено");

                        if (await IsEventOrganizerAsync(ev.Id))
                            return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя пожаловаться на собственное мероприятие");

                        report.EventId = ev.Id;
                        report.TargetSnapshot = JsonSerializer.Serialize(new
                        {
                            type = "event",
                            eventId = ev.Id,
                            name = ev.Name,
                            description = ev.Description,
                            active = ev.Active,
                            coverImageId = ev.CoverImageId
                        });
                        return CommandResult.OK;
                    }

                case ReportTargetType.Message:
                    {
                        var message = await _contentReportsRepository.GetMessageAsync(request.TargetId);
                        if (message == null)
                            return CommandResult.Fail(ErrorCode.MessageNotFound, "Сообщение не найдено");

                        if (message.AccountId != null && message.AccountId == _accountDataHolder.AccountId)
                            return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя пожаловаться на собственное сообщение");

                        var conversation = await _conversationRepository.GetConversationAsync(message.ConversationId);
                        if (conversation?.EventId == null)
                            return CommandResult.Fail(ErrorCode.InvalidValue, "Жалобы поддерживаются только для обсуждений мероприятий");

                        report.MessageId = message.Id;
                        report.ConversationId = message.ConversationId;
                        report.EventId = conversation.EventId;
                        report.ReportedAccountId = message.AccountId;
                        report.OrganizationId = message.OrganizationId;
                        report.TargetSnapshot = JsonSerializer.Serialize(new
                        {
                            type = "message",
                            messageId = message.Id,
                            conversationId = message.ConversationId,
                            eventId = conversation.EventId,
                            accountId = message.AccountId,
                            organizationId = message.OrganizationId,
                            messageText = message.MessageText,
                            createDate = message.CreateDate
                        });
                        return CommandResult.OK;
                    }

                case ReportTargetType.Photo:
                    {
                        var context = await _contentReportsRepository.ResolvePhotoContextAsync(
                            request.TargetId, request.AlbumId);
                        if (context == null)
                            return CommandResult.Fail(ErrorCode.AlbumItemNotFound, "Фото не найдено");

                        var isOwnProfilePhoto = context.AccountId != null
                            && context.AccountId == _accountDataHolder.AccountId
                            && (context.Kind == "account_album" || context.Kind == "account_avatar");
                        if (isOwnProfilePhoto)
                            return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя пожаловаться на собственное фото профиля");

                        report.FileId = context.FileId;
                        report.AlbumId = context.AlbumId ?? request.AlbumId;
                        report.EventId = context.EventId;
                        report.ReportedAccountId = context.AccountId;
                        report.OrganizationId = context.OrganizationId;
                        report.TargetSnapshot = JsonSerializer.Serialize(new
                        {
                            type = "photo",
                            kind = context.Kind,
                            fileId = context.FileId,
                            albumId = context.AlbumId,
                            eventId = context.EventId,
                            accountId = context.AccountId,
                            organizationId = context.OrganizationId
                        });
                        return CommandResult.OK;
                    }

                case ReportTargetType.Account:
                    {
                        if (request.TargetId == _accountDataHolder.AccountId)
                            return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя пожаловаться на собственный аккаунт");

                        var account = await _accountsRepository.GetAccountAsync(request.TargetId);
                        if (account == null)
                            return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

                        report.ReportedAccountId = account.Id;
                        report.TargetSnapshot = JsonSerializer.Serialize(new
                        {
                            type = "account",
                            accountId = account.Id,
                            login = account.Login,
                            active = account.Active,
                            avatarId = account.AvatarId
                        });
                        return CommandResult.OK;
                    }

                case ReportTargetType.Organization:
                    {
                        var organization = await _organizationsRepository.GetOrganizationAsync(request.TargetId);
                        if (organization == null)
                            return CommandResult.Fail(ErrorCode.OrganizationNotFound, "Организация не найдена");

                        report.OrganizationId = organization.Id;
                        report.TargetSnapshot = JsonSerializer.Serialize(new
                        {
                            type = "organization",
                            organizationId = organization.Id,
                            name = organization.Name,
                            active = organization.Active,
                            createdByAccountId = organization.CreatedByAccountId
                        });
                        return CommandResult.OK;
                    }

                case ReportTargetType.EventOrganizator:
                    {
                        var organizator = await _eventOrganizatorsRepository.GetByIdAsync(request.TargetId);
                        if (organizator == null)
                            return CommandResult.Fail(ErrorCode.InvalidValue, "Организатор мероприятия не найден");

                        if (organizator.AccountId != null && organizator.AccountId == _accountDataHolder.AccountId)
                            return CommandResult.Fail(ErrorCode.InvalidValue, "Нельзя пожаловаться на себя как на организатора");

                        report.EventOrganizatorId = organizator.Id;
                        report.EventId = organizator.EventId;
                        report.ReportedAccountId = organizator.AccountId;
                        report.OrganizationId = organizator.OrganizationId;
                        report.TargetSnapshot = JsonSerializer.Serialize(new
                        {
                            type = "event_organizator",
                            eventOrganizatorId = organizator.Id,
                            eventId = organizator.EventId,
                            accountId = organizator.AccountId,
                            organizationId = organizator.OrganizationId
                        });
                        return CommandResult.OK;
                    }

                default:
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректный тип цели");
            }
        }

        private async Task<bool> IsReportSubjectAsync(ContentReport report)
        {
            if (_accountDataHolder.AccountId == null)
                return false;

            var me = _accountDataHolder.AccountId.Value;

            if (report.ReportedAccountId == me)
                return true;

            switch (report.TargetType)
            {
                case ReportTargetType.Account:
                    return report.TargetId == me;

                case ReportTargetType.Event:
                    return report.EventId != null && await IsEventOrganizerAsync(report.EventId.Value);

                case ReportTargetType.Organization:
                    return report.OrganizationId != null
                        && await _organizationsRepository.IsActiveMemberAsync(report.OrganizationId.Value, me);

                case ReportTargetType.EventOrganizator:
                    if (report.ReportedAccountId == me)
                        return true;
                    return report.OrganizationId != null
                        && await _organizationsRepository.IsActiveMemberAsync(report.OrganizationId.Value, me);

                case ReportTargetType.Message:
                    return report.ReportedAccountId == me
                        || report.Message?.AccountId == me
                        || TryGetAccountIdFromSnapshot(report.TargetSnapshot) == me;

                case ReportTargetType.Photo:
                    if (report.ReportedAccountId == me)
                        return true;
                    if (report.OrganizationId != null
                        && await _organizationsRepository.IsActiveMemberAsync(report.OrganizationId.Value, me))
                        return true;
                    return false;

                default:
                    return false;
            }
        }

        private async Task<CommandResult> ApplyPhotoModerationAsync(ContentReport report, bool delete)
        {
            var kind = TryGetStringFromSnapshot(report.TargetSnapshot, "kind");
            var fileId = report.FileId ?? (report.TargetType == ReportTargetType.Photo ? report.TargetId : (Guid?)null);
            if (fileId == null)
                return CommandResult.Fail(ErrorCode.AlbumItemNotFound, "Фото не найдено");

            switch (kind)
            {
                case "event_cover":
                    if (report.EventId != null)
                        await _eventsRepository.SetEventCoverImageAsync(report.EventId.Value, null);
                    return CommandResult.OK;

                case "account_avatar":
                    await _mediaRepository.DeleteAvatarAsync(fileId.Value);
                    return CommandResult.OK;

                case "organization_avatar":
                    await _mediaRepository.DeleteOrganizationAvatarAsync(fileId.Value);
                    return CommandResult.OK;

                default:
                    if (delete)
                        await _contentReportsRepository.DeleteAlbumFileAsync(fileId.Value, report.AlbumId);
                    else
                        await _contentReportsRepository.SetAlbumFileHiddenAsync(
                            fileId.Value, report.AlbumId, true, _accountDataHolder.AccountId);
                    return CommandResult.OK;
            }
        }

        private async Task<CommandResult> ResetAvatarAsync(ContentReport report)
        {
            var kind = TryGetStringFromSnapshot(report.TargetSnapshot, "kind");
            var fileId = report.FileId;

            if (kind == "event_cover" || (report.TargetType == ReportTargetType.Photo && report.EventId != null && fileId != null && kind == "event_cover"))
            {
                if (report.EventId == null)
                    return CommandResult.Fail(ErrorCode.EventNotFound, "Мероприятие не найдено");
                await _eventsRepository.SetEventCoverImageAsync(report.EventId.Value, null);
                return CommandResult.OK;
            }

            if (kind == "account_avatar" || report.TargetType == ReportTargetType.Account)
            {
                if (fileId != null)
                {
                    await _mediaRepository.DeleteAvatarAsync(fileId.Value);
                    return CommandResult.OK;
                }

                var accountId = report.ReportedAccountId
                    ?? (report.TargetType == ReportTargetType.Account ? report.TargetId : (Guid?)null);
                if (accountId == null)
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Не удалось определить аккаунт для сброса аватарки");

                var last = await _mediaRepository.GetLastAccountAvatarAsync(accountId.Value);
                if (last != null)
                    await _mediaRepository.DeleteAvatarAsync(last.Value);
                return CommandResult.OK;
            }

            if (kind == "organization_avatar" || report.TargetType == ReportTargetType.Organization)
            {
                if (fileId != null)
                {
                    await _mediaRepository.DeleteOrganizationAvatarAsync(fileId.Value);
                    return CommandResult.OK;
                }

                var organizationId = report.OrganizationId
                    ?? (report.TargetType == ReportTargetType.Organization ? report.TargetId : (Guid?)null);
                if (organizationId == null)
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Не удалось определить организацию для сброса аватарки");

                var last = await _mediaRepository.GetLastOrganizationAvatarAsync(organizationId.Value);
                if (last != null)
                    await _mediaRepository.DeleteOrganizationAvatarAsync(last.Value);
                return CommandResult.OK;
            }

            return CommandResult.Fail(ErrorCode.InvalidValue, "Сброс аватарки не применим к этой жалобе");
        }

        private static string? TryGetStringFromSnapshot(string? snapshot, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(snapshot))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(snapshot);
                if (doc.RootElement.TryGetProperty(propertyName, out var prop)
                    && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
            }
            catch
            {
                // ignore malformed snapshot
            }

            return null;
        }

        private static Guid? TryGetAccountIdFromSnapshot(string? snapshot)
        {
            if (string.IsNullOrWhiteSpace(snapshot))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(snapshot);
                if (doc.RootElement.TryGetProperty("accountId", out var prop)
                    && prop.ValueKind != JsonValueKind.Null
                    && Guid.TryParse(prop.ToString(), out var id))
                    return id;
            }
            catch
            {
                // ignore malformed snapshot
            }

            return null;
        }

        private PagedList<ContentReportResponse> MapPaged(PagedList<ContentReport> result)
        {
            return new PagedList<ContentReportResponse>(
                result.Total,
                result.Result?.Select(i => _mapper.Map<ContentReportResponse>(i)).ToList()
                    ?? new List<ContentReportResponse>(),
                result.PageIndex,
                result.PageSize);
        }
    }
}
