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
        Task<CommandResult<PagedList<ContentReportResponse>>> SearchPlatformQueueAsync(ContentReportsSearchRequest request);
        Task<CommandResult<PagedList<ContentReportResponse>>> SearchOrganizerQueueAsync(Guid eventId, ContentReportsSearchRequest? request = null);
        Task<CommandResult<int>> CountPlatformQueueAsync(bool onlyActive = true);
        Task<CommandResult<int>> CountOrganizerQueueAsync(Guid eventId, bool onlyActive = true);

        Task<CommandResult> TakeInReviewAsync(Guid reportId);
        Task<CommandResult> ResolveAsync(Guid reportId, ResolveContentReportRequest request);
        Task<CommandResult> EscalateAsync(Guid reportId, EscalateContentReportRequest request);
        Task<CommandResult<List<ContentReportAction>>> GetActionsAsync(Guid reportId);
    }
}
