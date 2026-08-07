using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.Models.BugReports;

namespace EList.DbDataProvider.Interfaces
{
    public interface IBugReportsDataProvider
    {
        Task<List<BugReportCategoryDto>> GetCategoriesAsync(bool onlyActive = true);
        Task<BugReportCategoryDto?> GetCategoryByIdAsync(Guid id);
        Task<Guid> CreateCategoryAsync(BugReportCategoryDto item);
        Task UpdateCategoryAsync(BugReportCategoryDto item);
        Task SetCategoryActiveAsync(Guid id, bool active);
        Task<bool> CategoryCodeExistsAsync(string code, Guid? excludeId = null);
        Task<int> CountReportsByCategoryAsync(Guid categoryId);
        Task DeleteCategoryAsync(Guid id);

        Task<Guid> CreateReportAsync(BugReportDto item, List<Guid>? fileIds);
        Task<BugReportDto?> GetReportByIdAsync(Guid id);
        Task<ListResponse<BugReportDto>> SearchReportsAsync(
            Guid? categoryId,
            BugReportStatus? status,
            Guid? reporterAccountId,
            string? description,
            int? pageIndex,
            int? pageSize);
        Task SetReportStatusAsync(Guid id, BugReportStatus status);
    }
}
