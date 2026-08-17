using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IContentReportsDataProvider
    {
        #region reasons

        Task<List<ReportReasonDto>> GetReasonsAsync(
            bool onlyActive = true,
            ReportTargetType? forTargetType = null,
            ReportSeverity? severity = null);
        Task<ReportReasonDto?> GetReasonByIdAsync(Guid id);
        Task<ReportReasonDto?> GetReasonByCodeAsync(string code);
        Task<Guid> CreateReasonAsync(ReportReasonDto item);
        Task UpdateReasonAsync(ReportReasonDto item);
        Task SetReasonActiveAsync(Guid id, bool active);
        Task<bool> ReasonCodeExistsAsync(string code, Guid? excludeId = null);
        Task<int> CountReportsByReasonAsync(Guid reasonId);
        Task DeleteReasonAsync(Guid id);

        #endregion

        #region reports

        /// <summary>
        /// Заполняет status / organizer_status / platform_status по типу цели и причине.
        /// Event → platform; message+community → organizers; message+safety / both → both.
        /// </summary>
        void ApplyDefaultQueueStatuses(ContentReportDto report, ReportReasonDto reason);

        Task<Guid> CreateReportAsync(ContentReportDto item);
        Task<ContentReportDto?> GetReportByIdAsync(Guid id, bool includeActions = false);
        Task<ContentReportDto?> GetOpenReportByReporterAndTargetAsync(
            Guid reporterAccountId,
            ReportTargetType targetType,
            Guid targetId);
        Task<ListResponse<ContentReportDto>> SearchReportsAsync(ContentReportsSearchRequest request);
        Task<int> CountReportsAsync(ContentReportsSearchRequest request);
        Task<ListResponse<ContentReportDto>> SearchReportsConcerningAccountAsync(
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

        #endregion

        #region actions

        Task<Guid> AddActionAsync(ContentReportActionDto action);
        Task<List<ContentReportActionDto>> GetActionsByReportIdAsync(Guid reportId);

        #endregion

        #region message moderation state

        Task SetMessageHiddenAsync(Guid messageId, bool hidden, Guid? hiddenBy);
        Task<MessageDto?> GetMessageAsync(Guid messageId);

        Task<PhotoReportContextDto?> ResolvePhotoContextAsync(Guid fileId, Guid? albumId);
        Task SetAlbumFileHiddenAsync(Guid fileId, Guid? albumId, bool hidden, Guid? hiddenBy);
        Task DeleteAlbumFileAsync(Guid fileId, Guid? albumId);
        Task<Guid?> GetEventIdByCoverImageAsync(Guid fileId);

        #endregion
    }
}
