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
                var target = forTargetType.Value;
                query = query.Where(i =>
                    i.TargetScope == ReportTargetScope.All
                    || (i.TargetScope == ReportTargetScope.Both && (target == ReportTargetType.Event || target == ReportTargetType.Message))
                    || (i.TargetScope == ReportTargetScope.Event && target == ReportTargetType.Event)
                    || (i.TargetScope == ReportTargetScope.Message && target == ReportTargetType.Message)
                    || (i.TargetScope == ReportTargetScope.Photo && target == ReportTargetType.Photo)
                    || (i.TargetScope == ReportTargetScope.Account && target == ReportTargetType.Account)
                    || (i.TargetScope == ReportTargetScope.Organization && target == ReportTargetType.Organization)
                    || (i.TargetScope == ReportTargetScope.EventOrganizator && target == ReportTargetType.EventOrganizator));
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
            var platformOnly =
                report.TargetType == ReportTargetType.Event
                || report.TargetType == ReportTargetType.Account
                || report.TargetType == ReportTargetType.Organization
                || report.TargetType == ReportTargetType.EventOrganizator
                || (report.TargetType == ReportTargetType.Photo && report.EventId == null);

            if (platformOnly)
            {
                report.PlatformStatus = ReportStatus.Open;
                report.OrganizerStatus = null;
                report.Status = ReportStatus.Open;
                return;
            }

            // Safety / both → параллельные очереди (сообщение или фото события).
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

            // Community message / event photo → организаторы.
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
                .LoadWith(i => i.ReportedAccount)
                .ThenLoad(a => a.PersonInfo)
                .LoadWith(i => i.Organization)
                .LoadWith(i => i.EventOrganizator)
                .LoadWith(i => i.Album)
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

        public async Task<ListResponse<ContentReportDto>> SearchReportsConcerningAccountAsync(
            Guid accountId,
            List<Guid> organizationIds,
            int pageIndex,
            int pageSize)
        {
            var orgIds = organizationIds ?? new List<Guid>();
            var query = _connection.ContentReports
                .LoadWith(i => i.Reason)
                .LoadWith(i => i.Event)
                .AsQueryable();

            if (orgIds.Count == 0)
            {
                query = query.Where(i =>
                    i.ReportedAccountId == accountId
                    || (i.TargetType == ReportTargetType.Account && i.TargetId == accountId));
            }
            else
            {
                query = query.Where(i =>
                    i.ReportedAccountId == accountId
                    || (i.TargetType == ReportTargetType.Account && i.TargetId == accountId)
                    || (i.OrganizationId != null && orgIds.Contains(i.OrganizationId.Value))
                    || (i.TargetType == ReportTargetType.Organization && orgIds.Contains(i.TargetId)));
            }

            query = query.OrderByDescending(i => i.CreatedAt);
            var totalCount = await query.CountAsync();
            var pageSz = Math.Max(pageSize, 1);
            var items = await query.Skip(pageIndex * pageSz).Take(pageSz).ToListAsync();
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

        public async Task<PhotoReportContextDto?> ResolvePhotoContextAsync(Guid fileId, Guid? albumId)
        {
            FileAlbumRelationDto? albumFile = null;
            if (albumId != null)
            {
                albumFile = await _connection.AlbumFiles
                    .FirstOrDefaultAsync(i => i.FileId == fileId && i.AlbumId == albumId.Value);
            }
            else
            {
                var eventAlbumFile = await (
                    from f in _connection.AlbumFiles
                    join e in _connection.EventAlbums on f.AlbumId equals e.AlbumId
                    where f.FileId == fileId
                    select f
                ).FirstOrDefaultAsync();
                albumFile = eventAlbumFile
                    ?? await _connection.AlbumFiles.FirstOrDefaultAsync(i => i.FileId == fileId);
            }

            if (albumFile != null)
            {
                var eventRelation = await _connection.EventAlbums
                    .FirstOrDefaultAsync(i => i.AlbumId == albumFile.AlbumId);
                if (eventRelation != null)
                {
                    return new PhotoReportContextDto
                    {
                        FileId = fileId,
                        AlbumId = albumFile.AlbumId,
                        EventId = eventRelation.EventId,
                        Kind = "event_album"
                    };
                }

                var accountRelation = await _connection.AccountAlbums
                    .FirstOrDefaultAsync(i => i.AlbumId == albumFile.AlbumId);
                if (accountRelation != null)
                {
                    return new PhotoReportContextDto
                    {
                        FileId = fileId,
                        AlbumId = albumFile.AlbumId,
                        AccountId = accountRelation.AccountId,
                        Kind = "account_album"
                    };
                }
            }

            var eventByCover = await _connection.Events.FirstOrDefaultAsync(i => i.CoverImageId == fileId);
            if (eventByCover != null)
            {
                return new PhotoReportContextDto
                {
                    FileId = fileId,
                    EventId = eventByCover.Id,
                    Kind = "event_cover"
                };
            }

            var accountAvatar = await _connection.AccountAvatars
                .OrderByDescending(i => i.AssignmentDate)
                .FirstOrDefaultAsync(i => i.PhotoId == fileId);
            if (accountAvatar != null)
            {
                return new PhotoReportContextDto
                {
                    FileId = fileId,
                    AccountId = accountAvatar.AccountId,
                    Kind = "account_avatar"
                };
            }

            var orgAvatar = await _connection.OrganizationAvatars
                .OrderByDescending(i => i.AssignmentDate)
                .FirstOrDefaultAsync(i => i.PhotoId == fileId);
            if (orgAvatar != null)
            {
                return new PhotoReportContextDto
                {
                    FileId = fileId,
                    OrganizationId = orgAvatar.OrganizationId,
                    Kind = "organization_avatar"
                };
            }

            return albumFile == null
                ? null
                : new PhotoReportContextDto
                {
                    FileId = fileId,
                    AlbumId = albumFile.AlbumId,
                    Kind = "album"
                };
        }

        public async Task SetAlbumFileHiddenAsync(Guid fileId, Guid? albumId, bool hidden, Guid? hiddenBy)
        {
            var query = _connection.AlbumFiles.Where(i => i.FileId == fileId);
            if (albumId != null)
                query = query.Where(i => i.AlbumId == albumId.Value);

            if (hidden)
            {
                await query
                    .Set(i => i.Hidden, true)
                    .Set(i => i.HiddenAt, DateTimeOffset.UtcNow)
                    .Set(i => i.HiddenBy, hiddenBy)
                    .UpdateAsync();
            }
            else
            {
                await query
                    .Set(i => i.Hidden, false)
                    .Set(i => i.HiddenAt, (DateTimeOffset?)null)
                    .Set(i => i.HiddenBy, (Guid?)null)
                    .UpdateAsync();
            }
        }

        public async Task DeleteAlbumFileAsync(Guid fileId, Guid? albumId)
        {
            var query = _connection.AlbumFiles.Where(i => i.FileId == fileId);
            if (albumId != null)
                query = query.Where(i => i.AlbumId == albumId.Value);

            await query.DeleteAsync();
        }

        public async Task<Guid?> GetEventIdByCoverImageAsync(Guid fileId)
        {
            var ev = await _connection.Events.FirstOrDefaultAsync(i => i.CoverImageId == fileId);
            return ev?.Id;
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

            if (request.FileId != null)
                query = query.Where(i => i.FileId == request.FileId);

            if (request.ReportedAccountId != null)
                query = query.Where(i => i.ReportedAccountId == request.ReportedAccountId);

            if (request.OrganizationId != null)
                query = query.Where(i => i.OrganizationId == request.OrganizationId);

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

        public async Task<ContentReportTargetStatsDto> GetTargetStatsAsync(ReportTargetType targetType, Guid targetId)
        {
            var direct = _connection.ContentReports
                .Where(i => i.TargetType == targetType && i.TargetId == targetId);

            var stats = new ContentReportTargetStatsDto
            {
                TotalReports = await direct.CountAsync(),
                OpenReports = await direct.CountAsync(i => ActiveStatuses.Contains(i.Status)),
                ResolvedReports = await direct.CountAsync(i => i.Status == ReportStatus.Resolved),
                DismissedReports = await direct.CountAsync(i => i.Status == ReportStatus.Dismissed),
                WarningCount = await direct.CountAsync(i => i.ResolutionAction == ReportResolutionAction.Warn)
            };

            stats.LastWarningAt = await direct
                .Where(i => i.ResolutionAction == ReportResolutionAction.Warn)
                .OrderByDescending(i => i.ResolvedAt)
                .Select(i => i.ResolvedAt)
                .FirstOrDefaultAsync();

            stats.LastReportAt = await direct
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => (DateTimeOffset?)i.CreatedAt)
                .FirstOrDefaultAsync();

            IQueryable<ContentReportDto>? related = null;
            if (targetType == ReportTargetType.Event)
            {
                related = _connection.ContentReports.Where(i =>
                    i.EventId == targetId
                    && (i.TargetType != ReportTargetType.Event || i.TargetId != targetId));
            }
            else if (targetType == ReportTargetType.Account)
            {
                related = _connection.ContentReports.Where(i =>
                    i.ReportedAccountId == targetId
                    && (i.TargetType != ReportTargetType.Account || i.TargetId != targetId));
            }
            else if (targetType == ReportTargetType.Organization)
            {
                related = _connection.ContentReports.Where(i =>
                    i.OrganizationId == targetId
                    && (i.TargetType != ReportTargetType.Organization || i.TargetId != targetId));
            }

            if (related != null)
            {
                stats.RelatedTotalReports = await related.CountAsync();
                stats.RelatedOpenReports = await related.CountAsync(i => ActiveStatuses.Contains(i.Status));
                stats.RelatedWarningCount = await related.CountAsync(i => i.ResolutionAction == ReportResolutionAction.Warn);
            }

            return stats;
        }
    }
}
