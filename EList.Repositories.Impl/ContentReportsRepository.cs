using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.ContentReports;
using EList.Models.Conversations;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class ContentReportsRepository : IContentReportsRepository
    {
        private readonly IContentReportsDataProvider _dataProvider;
        private readonly IMapper _mapper;

        public ContentReportsRepository(IContentReportsDataProvider dataProvider, IMapper mapper)
        {
            _dataProvider = dataProvider;
            _mapper = mapper;
        }

        public async Task<List<ReportReason>> GetReasonsAsync(
            bool onlyActive = true,
            ReportTargetType? forTargetType = null,
            ReportSeverity? severity = null)
        {
            var dbTarget = forTargetType == null
                ? (DbDataProvider.Models.Enums.ReportTargetType?)null
                : _mapper.Map<DbDataProvider.Models.Enums.ReportTargetType>(forTargetType.Value);
            var dbSeverity = severity == null
                ? (DbDataProvider.Models.Enums.ReportSeverity?)null
                : _mapper.Map<DbDataProvider.Models.Enums.ReportSeverity>(severity.Value);

            var items = await _dataProvider.GetReasonsAsync(onlyActive, dbTarget, dbSeverity);
            return _mapper.Map<List<ReportReason>>(items);
        }

        public async Task<ReportReason?> GetReasonByIdAsync(Guid id)
        {
            var item = await _dataProvider.GetReasonByIdAsync(id);
            return _mapper.Map<ReportReason>(item);
        }

        public async Task<ReportReason?> GetReasonByCodeAsync(string code)
        {
            var item = await _dataProvider.GetReasonByCodeAsync(code);
            return _mapper.Map<ReportReason>(item);
        }

        public async Task<Guid> CreateReasonAsync(ReportReason reason)
        {
            var mapped = _mapper.Map<ReportReasonDto>(reason);
            return await _dataProvider.CreateReasonAsync(mapped);
        }

        public async Task UpdateReasonAsync(ReportReason reason)
        {
            var mapped = _mapper.Map<ReportReasonDto>(reason);
            await _dataProvider.UpdateReasonAsync(mapped);
        }

        public async Task SetReasonActiveAsync(Guid id, bool active)
        {
            await _dataProvider.SetReasonActiveAsync(id, active);
        }

        public async Task<bool> ReasonCodeExistsAsync(string code, Guid? excludeId = null)
        {
            return await _dataProvider.ReasonCodeExistsAsync(code, excludeId);
        }

        public async Task<int> CountReportsByReasonAsync(Guid reasonId)
        {
            return await _dataProvider.CountReportsByReasonAsync(reasonId);
        }

        public async Task DeleteReasonAsync(Guid id)
        {
            await _dataProvider.DeleteReasonAsync(id);
        }

        public void ApplyDefaultQueueStatuses(ContentReport report, ReportReason reason)
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

            report.OrganizerStatus = ReportStatus.Open;
            report.PlatformStatus = null;
            report.Status = ReportStatus.Open;
        }

        public async Task<Guid> CreateReportAsync(ContentReport report)
        {
            var mapped = _mapper.Map<ContentReportDto>(report);
            return await _dataProvider.CreateReportAsync(mapped);
        }

        public async Task<ContentReport?> GetReportByIdAsync(Guid id, bool includeActions = false)
        {
            var item = await _dataProvider.GetReportByIdAsync(id, includeActions);
            return MapReport(item);
        }

        public async Task<ContentReport?> GetOpenReportByReporterAndTargetAsync(
            Guid reporterAccountId,
            ReportTargetType targetType,
            Guid targetId)
        {
            var dbTarget = _mapper.Map<DbDataProvider.Models.Enums.ReportTargetType>(targetType);
            var item = await _dataProvider.GetOpenReportByReporterAndTargetAsync(reporterAccountId, dbTarget, targetId);
            return MapReport(item);
        }

        public async Task<PagedList<ContentReport>> SearchReportsAsync(ContentReportsSearchRequest request)
        {
            var dbRequest = _mapper.Map<DbDataProvider.Models.SearchRequests.ContentReportsSearchRequest>(request);
            var result = await _dataProvider.SearchReportsAsync(dbRequest);
            var items = result.Items?.Select(MapReport).Where(i => i != null).Cast<ContentReport>().ToList()
                ?? new List<ContentReport>();

            return new PagedList<ContentReport>(
                result.TotalCount,
                items,
                request.PageIndex ?? 0,
                request.PageSize ?? Math.Max(result.TotalCount, 1));
        }

        public async Task<int> CountReportsAsync(ContentReportsSearchRequest request)
        {
            var dbRequest = _mapper.Map<DbDataProvider.Models.SearchRequests.ContentReportsSearchRequest>(request);
            return await _dataProvider.CountReportsAsync(dbRequest);
        }

        public async Task SetReportStatusAsync(Guid id, ReportStatus status)
        {
            await _dataProvider.SetReportStatusAsync(id, _mapper.Map<DbDataProvider.Models.Enums.ReportStatus>(status));
        }

        public async Task SetOrganizerStatusAsync(Guid id, ReportStatus status)
        {
            await _dataProvider.SetOrganizerStatusAsync(id, _mapper.Map<DbDataProvider.Models.Enums.ReportStatus>(status));
        }

        public async Task SetPlatformStatusAsync(Guid id, ReportStatus status)
        {
            await _dataProvider.SetPlatformStatusAsync(id, _mapper.Map<DbDataProvider.Models.Enums.ReportStatus>(status));
        }

        public async Task AssignReportAsync(Guid id, Guid? assignedTo)
        {
            await _dataProvider.AssignReportAsync(id, assignedTo);
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
            await _dataProvider.ResolveReportAsync(
                id,
                _mapper.Map<DbDataProvider.Models.Enums.ReportStatus>(status),
                resolutionAction == null
                    ? null
                    : _mapper.Map<DbDataProvider.Models.Enums.ReportResolutionAction>(resolutionAction.Value),
                resolutionComment,
                resolvedBy,
                organizerStatus == null
                    ? null
                    : _mapper.Map<DbDataProvider.Models.Enums.ReportStatus>(organizerStatus.Value),
                platformStatus == null
                    ? null
                    : _mapper.Map<DbDataProvider.Models.Enums.ReportStatus>(platformStatus.Value));
        }

        public async Task EscalateToPlatformAsync(Guid id, Guid? actorAccountId, string? comment)
        {
            await _dataProvider.EscalateToPlatformAsync(id, actorAccountId, comment);
        }

        public async Task DeleteReportAsync(Guid id)
        {
            await _dataProvider.DeleteReportAsync(id);
        }

        public async Task<Guid> AddActionAsync(ContentReportAction action)
        {
            var mapped = _mapper.Map<ContentReportActionDto>(action);
            return await _dataProvider.AddActionAsync(mapped);
        }

        public async Task<List<ContentReportAction>> GetActionsByReportIdAsync(Guid reportId)
        {
            var items = await _dataProvider.GetActionsByReportIdAsync(reportId);
            return items.Select(MapAction).Where(i => i != null).Cast<ContentReportAction>().ToList();
        }

        public async Task SetMessageHiddenAsync(Guid messageId, bool hidden, Guid? hiddenBy)
        {
            await _dataProvider.SetMessageHiddenAsync(messageId, hidden, hiddenBy);
        }

        public async Task<Message?> GetMessageAsync(Guid messageId)
        {
            var item = await _dataProvider.GetMessageAsync(messageId);
            if (item == null)
                return null;

            var message = _mapper.Map<Message>(item);
            if (item.Account != null)
            {
                message.Account = _mapper.Map<AccountPublicData>(item.Account);
                if (item.Account.PersonInfo != null)
                    message.PersonInfo = _mapper.Map<Models.Person.PersonInfo>(item.Account.PersonInfo);
            }
            return message;
        }

        public async Task<PhotoReportContext?> ResolvePhotoContextAsync(Guid fileId, Guid? albumId)
        {
            var item = await _dataProvider.ResolvePhotoContextAsync(fileId, albumId);
            if (item == null)
                return null;

            return new PhotoReportContext
            {
                FileId = item.FileId,
                AlbumId = item.AlbumId,
                EventId = item.EventId,
                AccountId = item.AccountId,
                OrganizationId = item.OrganizationId,
                Kind = item.Kind
            };
        }

        public async Task SetAlbumFileHiddenAsync(Guid fileId, Guid? albumId, bool hidden, Guid? hiddenBy)
        {
            await _dataProvider.SetAlbumFileHiddenAsync(fileId, albumId, hidden, hiddenBy);
        }

        public async Task DeleteAlbumFileAsync(Guid fileId, Guid? albumId)
        {
            await _dataProvider.DeleteAlbumFileAsync(fileId, albumId);
        }

        private ContentReport? MapReport(ContentReportDto? item)
        {
            if (item == null)
                return null;

            var report = _mapper.Map<ContentReport>(item);
            report.Reason = _mapper.Map<ReportReason>(item.Reason);

            if (item.ReporterAccount != null)
                report.Reporter = _mapper.Map<AccountPublicData>(item.ReporterAccount);
            if (item.AssignedToAccount != null)
                report.AssignedToAccount = _mapper.Map<AccountPublicData>(item.AssignedToAccount);
            if (item.ResolvedByAccount != null)
                report.ResolvedByAccount = _mapper.Map<AccountPublicData>(item.ResolvedByAccount);
            if (item.Event != null)
                report.Event = _mapper.Map<EventShort>(item.Event);
            if (item.Message != null)
                report.Message = _mapper.Map<Message>(item.Message);
            if (item.Actions != null)
                report.Actions = item.Actions.Select(MapAction).Where(i => i != null).Cast<ContentReportAction>().ToList();

            return report;
        }

        private ContentReportAction? MapAction(ContentReportActionDto? item)
        {
            if (item == null)
                return null;

            var action = _mapper.Map<ContentReportAction>(item);
            if (item.ActorAccount != null)
                action.ActorAccount = _mapper.Map<AccountPublicData>(item.ActorAccount);
            return action;
        }
    }
}
