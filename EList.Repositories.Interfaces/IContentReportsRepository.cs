using EList.Common.Models;
using EList.Models.ContentReports;
using EList.Models.Conversations;
using EList.Models.Enums;

namespace EList.Repositories.Interfaces
{
    public interface IContentReportsRepository
    {
        Task<List<ReportReason>> GetReasonsAsync(
            bool onlyActive = true,
            ReportTargetType? forTargetType = null,
            ReportSeverity? severity = null);
        Task<ReportReason?> GetReasonByIdAsync(Guid id);
        Task<ReportReason?> GetReasonByCodeAsync(string code);
        Task<Guid> CreateReasonAsync(ReportReason reason);
        Task UpdateReasonAsync(ReportReason reason);
        Task SetReasonActiveAsync(Guid id, bool active);
        Task<bool> ReasonCodeExistsAsync(string code, Guid? excludeId = null);
        Task<int> CountReportsByReasonAsync(Guid reasonId);
        Task DeleteReasonAsync(Guid id);

        void ApplyDefaultQueueStatuses(ContentReport report, ReportReason reason);
        Task<Guid> CreateReportAsync(ContentReport report);
        Task<ContentReport?> GetReportByIdAsync(Guid id, bool includeActions = false);
        Task<ContentReport?> GetOpenReportByReporterAndTargetAsync(
            Guid reporterAccountId,
            ReportTargetType targetType,
            Guid targetId);
        Task<PagedList<ContentReport>> SearchReportsAsync(ContentReportsSearchRequest request);
        Task<int> CountReportsAsync(ContentReportsSearchRequest request);
        Task<PagedList<ContentReport>> SearchReportsConcerningAccountAsync(
            Guid accountId,
            List<Guid> organizationIds,
            int pageIndex,
            int pageSize);

        Task SetReportStatusAsync(Guid id, ReportStatus status);
        Task SetOrganizerStatusAsync(Guid id, ReportStatus status);
        Task SetPlatformStatusAsync(Guid id, ReportStatus status);
        Task AssignReportAsync(Guid id, Guid? assignedTo);
        Task ResolveReportAsync(
            Guid id,
            ReportStatus status,
            ReportResolutionAction? resolutionAction,
            string? resolutionComment,
            Guid resolvedBy,
            ReportStatus? organizerStatus = null,
            ReportStatus? platformStatus = null);
        Task EscalateToPlatformAsync(Guid id, Guid? actorAccountId, string? comment);
        Task DeleteReportAsync(Guid id);

        Task<Guid> AddActionAsync(ContentReportAction action);
        Task<List<ContentReportAction>> GetActionsByReportIdAsync(Guid reportId);

        Task SetMessageHiddenAsync(Guid messageId, bool hidden, Guid? hiddenBy);
        Task<Message?> GetMessageAsync(Guid messageId);

        Task<PhotoReportContext?> ResolvePhotoContextAsync(Guid fileId, Guid? albumId);
        Task SetAlbumFileHiddenAsync(Guid fileId, Guid? albumId, bool hidden, Guid? hiddenBy);
        Task DeleteAlbumFileAsync(Guid fileId, Guid? albumId);
    }
}
