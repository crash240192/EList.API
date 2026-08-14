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
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMapper _mapper;

        public ContentReportsService(
            IContentReportsRepository contentReportsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IEventsRepository eventsRepository,
            IConversationRepository conversationRepository,
            IParticipantsBWListRepository participantsBWListRepository,
            IAccountDataHolder accountDataHolder,
            ICorrelationIdProvider correlationIdProvider,
            IMapper mapper)
        {
            _contentReportsRepository = contentReportsRepository ?? throw new ArgumentNullException(nameof(contentReportsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _participantsBWListRepository = participantsBWListRepository ?? throw new ArgumentNullException(nameof(participantsBWListRepository));
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
                Comment = request.Comment?.Trim()
            };

            if (request.TargetType == ReportTargetType.Event)
            {
                var ev = await _eventsRepository.GetEventAsync(request.TargetId);
                if (ev == null)
                    return CommandResult<Guid?>.Fail(ErrorCode.EventNotFound, "Событие не найдено");

                report.EventId = ev.Id;
                report.TargetSnapshot = JsonSerializer.Serialize(new
                {
                    type = "event",
                    eventId = ev.Id,
                    name = ev.Name,
                    description = ev.Description,
                    active = ev.Active
                });
            }
            else
            {
                var message = await _contentReportsRepository.GetMessageAsync(request.TargetId);
                if (message == null)
                    return CommandResult<Guid?>.Fail(ErrorCode.MessageNotFound, "Сообщение не найдено");

                var conversation = await _conversationRepository.GetConversationAsync(message.ConversationId);
                if (conversation?.EventId == null)
                    return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue, "Жалобы поддерживаются только для обсуждений мероприятий");

                report.MessageId = message.Id;
                report.ConversationId = message.ConversationId;
                report.EventId = conversation.EventId;
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
            }

            _contentReportsRepository.ApplyDefaultQueueStatuses(report, reason);
            var reportId = await _contentReportsRepository.CreateReportAsync(report);

            await _contentReportsRepository.AddActionAsync(new ContentReportAction
            {
                ReportId = reportId,
                ActorAccountId = _accountDataHolder.AccountId,
                ActorContext = ReportActorContext.Reporter,
                Action = "created",
                Details = JsonSerializer.Serialize(new { reasonCode = reason.Code, targetType = request.TargetType.ToString() })
            });

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

            if (request.ResolutionAction == ReportResolutionAction.CancelEvent && !asPlatform)
                return CommandResult.Fail(ErrorCode.AccessError, "Отменить мероприятие может только модератор площадки");

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
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Скрытие поддерживается для сообщений");

                case ReportResolutionAction.DeleteContent:
                    if (report.TargetType == ReportTargetType.Message && report.MessageId != null)
                    {
                        await _contentReportsRepository.SetMessageHiddenAsync(
                            report.MessageId.Value, true, _accountDataHolder.AccountId);
                        await _conversationRepository.DeleteMessageAsync(report.MessageId.Value);
                        return CommandResult.OK;
                    }
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Удаление контента поддерживается для сообщений");

                case ReportResolutionAction.BanFromEvent:
                    if (report.EventId == null)
                        return CommandResult.Fail(ErrorCode.InvalidValue, "Не указано мероприятие для бана");

                    var accountId = request.TargetAccountId
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
                    return CommandResult.OK;

                case ReportResolutionAction.CancelEvent:
                    if (!asPlatform)
                        return CommandResult.Fail(ErrorCode.AccessError, "Отменить мероприятие может только модератор площадки");
                    if (report.EventId == null)
                        return CommandResult.Fail(ErrorCode.EventNotFound, "Мероприятие не найдено");

                    await _eventsRepository.CancelEventAsync(report.EventId.Value);
                    return CommandResult.OK;

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

            if (report.EventId != null && await IsEventOrganizerAsync(report.EventId.Value))
                return report.OrganizerStatus != null || report.PlatformStatus != null;

            return false;
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
            return reason.TargetScope == ReportTargetScope.Both
                || (targetType == ReportTargetType.Event && reason.TargetScope == ReportTargetScope.Event)
                || (targetType == ReportTargetType.Message && reason.TargetScope == ReportTargetScope.Message);
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
