using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.BugReports;
using EList.Models.Enums;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class BugReportsRepository : IBugReportsRepository
    {
        private readonly IBugReportsDataProvider _bugReportsDataProvider;
        private readonly IMapper _mapper;

        public BugReportsRepository(IBugReportsDataProvider bugReportsDataProvider, IMapper mapper)
        {
            _bugReportsDataProvider = bugReportsDataProvider;
            _mapper = mapper;
        }

        public async Task<List<BugReportCategory>> GetCategoriesAsync(bool onlyActive = true)
        {
            var items = await _bugReportsDataProvider.GetCategoriesAsync(onlyActive);
            return _mapper.Map<List<BugReportCategory>>(items);
        }

        public async Task<BugReportCategory?> GetCategoryByIdAsync(Guid id)
        {
            var item = await _bugReportsDataProvider.GetCategoryByIdAsync(id);
            return _mapper.Map<BugReportCategory>(item);
        }

        public async Task<Guid> CreateCategoryAsync(BugReportCategory category)
        {
            var mapped = _mapper.Map<BugReportCategoryDto>(category);
            return await _bugReportsDataProvider.CreateCategoryAsync(mapped);
        }

        public async Task UpdateCategoryAsync(BugReportCategory category)
        {
            var mapped = _mapper.Map<BugReportCategoryDto>(category);
            await _bugReportsDataProvider.UpdateCategoryAsync(mapped);
        }

        public async Task SetCategoryActiveAsync(Guid id, bool active)
        {
            await _bugReportsDataProvider.SetCategoryActiveAsync(id, active);
        }

        public async Task<bool> CategoryCodeExistsAsync(string code, Guid? excludeId = null)
        {
            return await _bugReportsDataProvider.CategoryCodeExistsAsync(code, excludeId);
        }

        public async Task<int> CountReportsByCategoryAsync(Guid categoryId)
        {
            return await _bugReportsDataProvider.CountReportsByCategoryAsync(categoryId);
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            await _bugReportsDataProvider.DeleteCategoryAsync(id);
        }

        public async Task<Guid> CreateReportAsync(BugReport report)
        {
            var mapped = _mapper.Map<BugReportDto>(report);
            mapped.Status = DbDataProvider.Models.Enums.BugReportStatus.Pending;
            return await _bugReportsDataProvider.CreateReportAsync(mapped, report.FileIds);
        }

        public async Task<BugReport?> GetReportByIdAsync(Guid id)
        {
            var item = await _bugReportsDataProvider.GetReportByIdAsync(id);
            return MapReport(item);
        }

        public async Task<PagedList<BugReport>> SearchReportsAsync(BugReportSearchRequest request)
        {
            var status = request.Status == null
                ? (DbDataProvider.Models.Enums.BugReportStatus?)null
                : _mapper.Map<DbDataProvider.Models.Enums.BugReportStatus>(request.Status.Value);

            var result = await _bugReportsDataProvider.SearchReportsAsync(
                request.CategoryId,
                status,
                request.ReporterAccountId,
                request.Description,
                request.PageIndex,
                request.PageSize);

            var items = result.Items?.Select(MapReport).Where(i => i != null).Cast<BugReport>().ToList()
                ?? new List<BugReport>();

            return new PagedList<BugReport>(
                result.TotalCount,
                items,
                request.PageIndex ?? 0,
                request.PageSize ?? Math.Max(result.TotalCount, 1));
        }

        public async Task SetReportStatusAsync(Guid id, BugReportStatus status)
        {
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.BugReportStatus>(status);
            await _bugReportsDataProvider.SetReportStatusAsync(id, mappedStatus);
        }

        private BugReport? MapReport(BugReportDto? item)
        {
            if (item == null)
                return null;

            var report = _mapper.Map<BugReport>(item);
            report.Category = _mapper.Map<BugReportCategory>(item.Category);
            report.FileIds = item.Files?.Select(f => f.FileId).ToList() ?? new List<Guid>();

            if (item.ReporterAccount != null)
            {
                report.Reporter = _mapper.Map<AccountPublicData>(item.ReporterAccount);
                if (item.ReporterAccount.PersonInfo != null && report.Reporter != null)
                {
                    // AccountPublicData may already include person fields via mapper
                }
            }

            return report;
        }
    }
}
