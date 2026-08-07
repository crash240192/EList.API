using EList.Common.Models;
using EList.Models.BugReports;

namespace EList.Services.Interfaces
{
    public interface IBugReportsService
    {
        Task<CommandResult<List<BugReportCategory>>> GetCategoriesAsync(bool onlyActive = true);
        Task<CommandResult<Guid?>> CreateCategoryAsync(CreateBugReportCategoryRequest request);

        Task<CommandResult<Guid?>> CreateReportAsync(CreateBugReportRequest request);
        Task<CommandResult<BugReportResponse?>> GetReportAsync(Guid reportId);
        Task<CommandResult<PagedList<BugReportResponse>>> GetMyReportsAsync(int? pageIndex = null, int? pageSize = null);
        Task<CommandResult<PagedList<BugReportResponse>>> SearchReportsAsync(BugReportSearchRequest request);
        Task<CommandResult> SetReportStatusAsync(Guid reportId, UpdateBugReportStatusRequest request);
    }
}
