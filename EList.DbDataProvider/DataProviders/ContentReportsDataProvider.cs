using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.DbDataProvider.Models.SearchRequests;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class ContentReportsDataProvider : DataProviderBase, IContentReportsDataProvider
    {
        private static readonly ReportStatus[] ActiveStatuses =
        {
            ReportStatus.Open,
            ReportStatus.InReview,
            ReportStatus.Escalated
        };

        public ContentReportsDataProvider(IDataConnectionProvider dataConnectionProvider)
            : base(dataConnectionProvider)
        {
        }

        #region reasons

        public async Task<List<ReportReasonDto>> GetReasonsAsync(
            bool onlyActive = true,
            ReportTargetType? forTargetType = null,
            ReportSeverity? severity = null)
        {
            var query = _connection.ReportReasons.AsQueryable();

            if (onlyActive)
                query = query.Where(i => i.Active);

            if (severity != null)
                query = query.Where(i => i.Severity == severity);

            if (forTargetType != null)
            {
                query = forTargetType == ReportTargetType.Event
                    ? query.Where(i => i.TargetScope == ReportTargetScope.Event || i.TargetScope == ReportTargetScope.Both)
                    : query.Where(i => i.TargetScope == ReportTargetScope.Message || i.TargetScope == ReportTargetScope.Both);
            }

            return await query.OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToListAsync();
        }

        public async Task<ReportReasonDto?> GetReasonByIdAsync(Guid id)
        {
            return await _connection.ReportReasons.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<ReportReasonDto?> GetReasonByCodeAsync(string code)
        {
            return await _connection.ReportReasons.FirstOrDefaultAsync(i => i.Code == code);
        }

        public async Task<Guid> CreateReasonAsync(ReportReasonDto item)
        {
            item.CreateDate = DateTimeOffset.UtcNow;
            item.Active = true;
            return (Guid)await _connection.InsertWithIdentityAsync(item);
        }

        public async Task UpdateReasonAsync(ReportReasonDto item)
        {
            await _connection.ReportReasons.Where(i => i.Id == item.Id)
                .Set(i => i.Code, item.Code)
                .Set(i => i.Name, item.Name)
                .Set(i => i.Description, item.Description)
                .Set(i => i.TargetScope, item.TargetScope)
                .Set(i => i.Severity, item.Severity)
                .Set(i => i.PrimaryQueue, item.PrimaryQueue)
                .Set(i => i.SortOrder, item.SortOrder)
                .Set(i => i.Active, item.Active)
                .UpdateAsync();
        }

        public async Task SetReasonActiveAsync(Guid id, bool active)
        {
            await _connection.ReportReasons.Where(i => i.Id == id)
                .Set(i => i.Active, active)
                .UpdateAsync();
        }

        public async Task<bool> ReasonCodeExistsAsync(string code, Guid? excludeId = null)
        {
            var query = _connection.ReportReasons.Where(i => i.Code == code);
            if (excludeId != null)
                query = query.Where(i => i.Id != excludeId);
            return await query.AnyAsync();
        }

        public async Task<int> CountReportsByReasonAsync(Guid reasonId)
        {
            return await _connection.ContentReports.CountAsync(i => i.ReasonId == reasonId);
        }

        public async Task DeleteReasonAsync(Guid id)
        {
            await _connection.ReportReasons.Where(i => i.Id == id).DeleteAsync();
        }

        #endregion

        #region reports

        public void ApplyDefaultQueueStatuses(ContentReportDto report, ReportReasonDto reason)
        {
            // События всегда ведёт платформа.
            if (report.TargetType == ReportTargetType.Event)
            {
                report.PlatformStatus = ReportStatus.Open;
                report.OrganizerStatus = null;
                report.Status = ReportStatus.Open;
                return;
            }

            // Safety / both → параллельные очереди.
            if (reason.Severity == ReportSeverity.Safety || reason.PrimaryQueue == ReportQueue.Both)
            {
                report.OrganizerStatus = ReportStatus.Open;
                report.PlatformStatus = ReportStatus.Open;
                report.Status = ReportStatus.Open;
                return;
            }

            if (reason.PrimaryQueue == ReportQueue.Platform)
            {
                report.PlatformStatus = ReportStatus.Open;
                report.OrganizerStatus = null;
                report.Status = ReportStatus.Open;
                return;
            }

            // Community message → организаторы.
            report.OrganizerStatus = ReportStatus.Open;
            report.PlatformStatus = null;
            report.Status = ReportStatus.Open;
        }

        public async Task<Guid> CreateReportAsync(ContentReportDto item)
        {
            var now = DateTimeOffset.UtcNow;
            item.CreatedAt = now;
            item.UpdatedAt = now;
            if (item.Status == default)
                item.Status = ReportStatus.Open;

            return (Guid)await _connection.InsertWithIdentityAsync(item);
        }

        public async Task<ContentReportDto?> GetReportByIdAsync(Guid id, bool includeActions = false)
        {
            var query = _connection.ContentReports
                .LoadWith(i => i.Reason)
                .LoadWith(i => i.ReporterAccount)
                .ThenLoad(a => a.PersonInfo)
                .LoadWith(i => i.Event)
                .LoadWith(i => i.Message)
                .LoadWith(i => i.Conversation)
                .LoadWith(i => i.AssignedToAccount)
                .ThenLoad(a => a.PersonInfo)
                .LoadWith(i => i.ResolvedByAccount)
                .ThenLoad(a => a.PersonInfo)
                .AsQueryable();

            if (includeActions)
                query = query.LoadWith(i => i.Actions);

            return await query.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<ContentReportDto?> GetOpenReportByReporterAndTargetAsync(
            Guid reporterAccountId,
            ReportTargetType targetType,
            Guid targetId)
        {
            return await _connection.ContentReports
                .FirstOrDefaultAsync(i =>
                    i.ReporterAccountId == reporterAccountId
                    && i.TargetType == targetType
                    && i.TargetId == targetId
                    && ActiveStatuses.Contains(i.Status));
        }

        public async Task<ListResponse<ContentReportDto>> SearchReportsAsync(ContentReportsSearchRequest request)
        {
            var query = _connection.ContentReports
                .LoadWith(i => i.Reason)
                .LoadWith(i => i.ReporterAccount)
                .ThenLoad(a => a.PersonInfo)
                .LoadWith(i => i.Event)
                .LoadWith(i => i.Message)
                .AsQueryable();

            query = ApplySearchFilters(query, request);
            query = query.OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var pageIdx = request.PageIndex ?? 0;
            var pageSz = request.PageSize ?? Math.Max(totalCount, 1);

            var items = await query.Skip(pageIdx * pageSz).Take(pageSz).ToListAsync();
            return new ListResponse<ContentReportDto>(totalCount, items);
        }

        public async Task<int> CountReportsAsync(ContentReportsSearchRequest request)
        {
            var query = ApplySearchFilters(_connection.ContentReports.AsQueryable(), request);
            return await query.CountAsync();
        }

        public async Task SetReportStatusAsync(Guid id, ReportStatus status)
        {
            await _connection.ContentReports.Where(i => i.Id == id)
                .Set(i => i.Status, status)
                .Set(i => i.UpdatedAt, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }

        public async Task SetOrganizerStatusAsync(Guid id, ReportStatus status)
        {
            await _connection.ContentReports.Where(i => i.Id == id)
                .Set(i => i.OrganizerStatus, status)
                .Set(i => i.UpdatedAt, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }

        public async Task SetPlatformStatusAsync(Guid id, ReportStatus status)
        {
            await _connection.ContentReports.Where(i => i.Id == id)
                .Set(i => i.PlatformStatus, status)
                .Set(i => i.UpdatedAt, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }

        public async Task AssignReportAsync(Guid id, Guid? assignedTo)
        {
            await _connection.ContentReports.Where(i => i.Id == id)
                .Set(i => i.AssignedTo, assignedTo)
                .Set(i => i.Status, ReportStatus.InReview)
                .Set(i => i.UpdatedAt, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }

        public async Task ResolveReportAsync(
            Guid id,
            ReportStatus status,
            ReportResolutionAction? resolutionAction,
            string? resolutionComment,
            Guid resolvedBy,
            ReportStatus? organizerStatus = null,
            ReportStatus? platformStatus = null)
        {
            var now = DateTimeOffset.UtcNow;
            var update = _connection.ContentReports.Where(i => i.Id == id)
                .Set(i => i.Status, status)
                .Set(i => i.ResolutionAction, resolutionAction)
                .Set(i => i.ResolutionComment, resolutionComment)
                .Set(i => i.ResolvedBy, resolvedBy)
                .Set(i => i.ResolvedAt, now)
                .Set(i => i.UpdatedAt, now);

            if (organizerStatus != null)
                update = update.Set(i => i.OrganizerStatus, organizerStatus);

            if (platformStatus != null)
                update = update.Set(i => i.PlatformStatus, platformStatus);

            await update.UpdateAsync();
        }

        public async Task EscalateToPlatformAsync(Guid id, Guid? actorAccountId, string? comment)
        {
            var now = DateTimeOffset.UtcNow;
            await _connection.ContentReports.Where(i => i.Id == id)
                .Set(i => i.Status, ReportStatus.Escalated)
                .Set(i => i.OrganizerStatus, ReportStatus.Escalated)
                .Set(i => i.PlatformStatus, ReportStatus.Open)
                .Set(i => i.UpdatedAt, now)
                .UpdateAsync();

            await AddActionAsync(new ContentReportActionDto
            {
                ReportId = id,
                ActorAccountId = actorAccountId,
                ActorContext = ReportActorContext.Organizer,
                Action = "escalate",
                Details = string.IsNullOrWhiteSpace(comment)
                    ? null
                    : $"{{\"comment\":{System.Text.Json.JsonSerializer.Serialize(comment)}}}",
                CreatedAt = now
            });
        }

        public async Task DeleteReportAsync(Guid id)
        {
            await _connection.ContentReportActions.Where(i => i.ReportId == id).DeleteAsync();
            await _connection.ContentReports.Where(i => i.Id == id).DeleteAsync();
        }

        #endregion

        #region actions

        public async Task<Guid> AddActionAsync(ContentReportActionDto action)
        {
            if (action.CreatedAt == default)
                action.CreatedAt = DateTimeOffset.UtcNow;

            return (Guid)await _connection.InsertWithIdentityAsync(action);
        }

        public async Task<List<ContentReportActionDto>> GetActionsByReportIdAsync(Guid reportId)
        {
            return await _connection.ContentReportActions
                .LoadWith(i => i.ActorAccount)
                .ThenLoad(a => a.PersonInfo)
                .Where(i => i.ReportId == reportId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        #endregion

        #region message moderation state

        public async Task SetMessageHiddenAsync(Guid messageId, bool hidden, Guid? hiddenBy)
        {
            if (hidden)
            {
                await _connection.Messages.Where(i => i.Id == messageId)
                    .Set(i => i.Hidden, true)
                    .Set(i => i.HiddenAt, DateTimeOffset.UtcNow)
                    .Set(i => i.HiddenBy, hiddenBy)
                    .Set(i => i.UpdateDate, DateTimeOffset.UtcNow)
                    .UpdateAsync();
            }
            else
            {
                await _connection.Messages.Where(i => i.Id == messageId)
                    .Set(i => i.Hidden, false)
                    .Set(i => i.HiddenAt, (DateTimeOffset?)null)
                    .Set(i => i.HiddenBy, (Guid?)null)
                    .Set(i => i.UpdateDate, DateTimeOffset.UtcNow)
                    .UpdateAsync();
            }
        }

        public async Task<MessageDto?> GetMessageAsync(Guid messageId)
        {
            return await _connection.Messages
                .LoadWith(i => i.Account)
                .ThenLoad(a => a.PersonInfo)
                .LoadWith(i => i.Conversation)
                .FirstOrDefaultAsync(i => i.Id == messageId);
        }

        #endregion

        private IQueryable<ContentReportDto> ApplySearchFilters(
            IQueryable<ContentReportDto> query,
            ContentReportsSearchRequest request)
        {
            if (request.TargetType != null)
                query = query.Where(i => i.TargetType == request.TargetType);

            if (request.TargetId != null)
                query = query.Where(i => i.TargetId == request.TargetId);

            if (request.EventId != null)
                query = query.Where(i => i.EventId == request.EventId);

            if (request.MessageId != null)
                query = query.Where(i => i.MessageId == request.MessageId);

            if (request.ReasonId != null)
                query = query.Where(i => i.ReasonId == request.ReasonId);

            if (request.Severity != null)
            {
                var severity = request.Severity.Value;
                var reasonIds = _connection.ReportReasons
                    .Where(r => r.Severity == severity)
                    .Select(r => r.Id);
                query = query.Where(i => reasonIds.Contains(i.ReasonId));
            }

            if (request.ReporterAccountId != null)
                query = query.Where(i => i.ReporterAccountId == request.ReporterAccountId);

            if (request.AssignedTo != null)
                query = query.Where(i => i.AssignedTo == request.AssignedTo);

            if (request.Status != null)
                query = query.Where(i => i.Status == request.Status);

            if (request.OrganizerStatus != null)
                query = query.Where(i => i.OrganizerStatus == request.OrganizerStatus);

            if (request.PlatformStatus != null)
                query = query.Where(i => i.PlatformStatus == request.PlatformStatus);

            if (request.InPlatformQueue == true)
                query = query.Where(i => i.PlatformStatus != null);

            if (request.InOrganizerQueue == true)
                query = query.Where(i => i.OrganizerStatus != null);

            if (request.OnlyActive == true)
                query = query.Where(i => ActiveStatuses.Contains(i.Status));

            return query;
        }
    }
}
