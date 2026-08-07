using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class BugReportsDataProvider : DataProviderBase, IBugReportsDataProvider
    {
        public BugReportsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<List<BugReportCategoryDto>> GetCategoriesAsync(bool onlyActive = true)
        {
            var query = _connection.BugReportCategories.AsQueryable();
            if (onlyActive)
                query = query.Where(i => i.Active);

            return await query.OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToListAsync();
        }

        public async Task<BugReportCategoryDto?> GetCategoryByIdAsync(Guid id)
        {
            return await _connection.BugReportCategories.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Guid> CreateCategoryAsync(BugReportCategoryDto item)
        {
            item.CreateDate = DateTimeOffset.UtcNow;
            item.Active = true;
            return (Guid)await _connection.InsertWithIdentityAsync(item);
        }

        public async Task UpdateCategoryAsync(BugReportCategoryDto item)
        {
            await _connection.BugReportCategories.Where(i => i.Id == item.Id)
                .Set(i => i.Code, item.Code)
                .Set(i => i.Name, item.Name)
                .Set(i => i.SortOrder, item.SortOrder)
                .Set(i => i.Active, item.Active)
                .UpdateAsync();
        }

        public async Task SetCategoryActiveAsync(Guid id, bool active)
        {
            await _connection.BugReportCategories.Where(i => i.Id == id)
                .Set(i => i.Active, active)
                .UpdateAsync();
        }

        public async Task<bool> CategoryCodeExistsAsync(string code, Guid? excludeId = null)
        {
            var query = _connection.BugReportCategories.Where(i => i.Code == code);
            if (excludeId != null)
                query = query.Where(i => i.Id != excludeId);
            return await query.AnyAsync();
        }

        public async Task<int> CountReportsByCategoryAsync(Guid categoryId)
        {
            return await _connection.BugReports.CountAsync(i => i.CategoryId == categoryId);
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            await _connection.BugReportCategories.Where(i => i.Id == id).DeleteAsync();
        }

        public async Task<Guid> CreateReportAsync(BugReportDto item, List<Guid>? fileIds)
        {
            var now = DateTimeOffset.UtcNow;
            item.CreateDate = now;
            item.UpdateDate = now;
            item.Status = BugReportStatus.Pending;

            var reportId = (Guid)await _connection.InsertWithIdentityAsync(item);

            if (fileIds?.Any() ?? false)
            {
                var files = fileIds.Distinct().Select(fileId => new BugReportFileDto
                {
                    ReportId = reportId,
                    FileId = fileId
                }).ToList();
                await _connection.BulkCopyAsync(files);
            }

            return reportId;
        }

        public async Task<BugReportDto?> GetReportByIdAsync(Guid id)
        {
            return await _connection.BugReports
                .LoadWith(i => i.Category)
                .LoadWith(i => i.ReporterAccount)
                .ThenLoad(a => a.PersonInfo)
                .LoadWith(i => i.Files)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<ListResponse<BugReportDto>> SearchReportsAsync(
            Guid? categoryId,
            BugReportStatus? status,
            Guid? reporterAccountId,
            string? description,
            int? pageIndex,
            int? pageSize)
        {
            var query = _connection.BugReports
                .LoadWith(i => i.Category)
                .LoadWith(i => i.ReporterAccount)
                .ThenLoad(a => a.PersonInfo)
                .LoadWith(i => i.Files)
                .AsQueryable();

            if (categoryId != null)
                query = query.Where(i => i.CategoryId == categoryId);

            if (status != null)
                query = query.Where(i => i.Status == status);

            if (reporterAccountId != null)
                query = query.Where(i => i.ReporterAccountId == reporterAccountId);

            if (!string.IsNullOrWhiteSpace(description))
                query = query.Where(i => i.Description.Contains(description));

            query = query.OrderByDescending(i => i.CreateDate);

            var totalCount = await query.CountAsync();
            var pageIdx = pageIndex ?? 0;
            var pageSz = pageSize ?? Math.Max(totalCount, 1);

            var items = await query.Skip(pageIdx * pageSz).Take(pageSz).ToListAsync();
            return new ListResponse<BugReportDto>(totalCount, items);
        }

        public async Task SetReportStatusAsync(Guid id, BugReportStatus status)
        {
            await _connection.BugReports.Where(i => i.Id == id)
                .Set(i => i.Status, status)
                .Set(i => i.UpdateDate, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }
    }
}
