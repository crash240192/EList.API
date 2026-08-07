using EList.Common.Models;
using EList.Models.BugReports;
using EList.Models.Enums;

namespace EList.Repositories.Interfaces
{
    public interface IBugReportsRepository
    {
        Task<List<BugReportCategory>> GetCategoriesAsync(bool onlyActive = true);
        Task<BugReportCategory?> GetCategoryByIdAsync(Guid id);
        Task<Guid> CreateCategoryAsync(BugReportCategory category);

        Task<Guid> CreateReportAsync(BugReport report);
        Task<BugReport?> GetReportByIdAsync(Guid id);
        Task<PagedList<BugReport>> SearchReportsAsync(BugReportSearchRequest request);
        Task SetReportStatusAsync(Guid id, BugReportStatus status);
    }
}
