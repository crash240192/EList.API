using EList.Common.Models;
using EList.Models.ContentReports;
using EList.Models.Enums;

namespace EList.Services.Interfaces
{
    public interface IContentReportsService
    {
        Task<CommandResult<List<ReportReason>>> GetReasonsAsync(
            bool onlyActive = true,
            ReportTargetType? forTargetType = null,
            ReportSeverity? severity = null);
        Task<CommandResult<ReportReason?>> GetReasonAsync(Guid reasonId);
        Task<CommandResult<Guid?>> CreateReasonAsync(CreateReportReasonRequest request);
        Task<CommandResult> UpdateReasonAsync(Guid reasonId, UpdateReportReasonRequest request);
        Task<CommandResult> SetReasonActiveAsync(Guid reasonId, bool active);
        Task<CommandResult> DeleteReasonAsync(Guid reasonId);

        Task<CommandResult<Guid?>> CreateReportAsync(CreateContentReportRequest request);
        Task<CommandResult<ContentReportResponse?>> GetReportAsync(Guid reportId);
        Task<CommandResult<PagedList<ContentReportResponse>>> GetMyReportsAsync(int? pageIndex = null, int? pageSize = null);
        Task<CommandResult<PagedList<ContentReportSubjectView>>> GetReportsAgainstMeAsync(int? pageIndex = null, int? pageSize = null);
        Task<CommandResult<ContentReportSubjectView?>> GetReportAgainstMeAsync(Guid reportId);
        Task<CommandResult<PagedList<ContentReportResponse>>> SearchPlatformQueueAsync(ContentReportsSearchRequest request);
        Task<CommandResult<PagedList<ContentReportResponse>>> SearchOrganizerQueueAsync(Guid eventId, ContentReportsSearchRequest? request = null);
        Task<CommandResult<int>> CountPlatformQueueAsync(bool onlyActive = true);
        Task<CommandResult<int>> CountOrganizerQueueAsync(Guid eventId, bool onlyActive = true);

        Task<CommandResult<ContentReportTargetStats>> GetTargetStatsAsync(ReportTargetType targetType, Guid targetId);
        Task<CommandResult<List<ModerationPenalty>>> GetMyPenaltiesAsync();
        Task<CommandResult> RevokePenaltyAsync(Guid penaltyId, RevokeModerationPenaltyRequest? request = null);
        Task<CommandResult> RestoreEventAsync(Guid eventId, RestoreEventRequest? request = null);

        Task<CommandResult> TakeInReviewAsync(Guid reportId);
        Task<CommandResult> ResolveAsync(Guid reportId, ResolveContentReportRequest request);
        Task<CommandResult> EscalateAsync(Guid reportId, EscalateContentReportRequest request);
        Task<CommandResult<List<ContentReportAction>>> GetActionsAsync(Guid reportId);

        /// <summary>
        /// Staff proxy: download reported file via filestorage service-token (works when Blocked).
        /// </summary>
        Task<CommandResult<ReportedFileContent>> DownloadReportedFileAsync(Guid reportId, bool? fullSize = null);

        /// <summary>
        /// Restore Active accessStatus for the report file (after mistaken Block).
        /// Also un-hides album relation when present.
        /// </summary>
        Task<CommandResult> RestoreReportedFileAccessAsync(Guid reportId);
    }
}
