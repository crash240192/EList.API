namespace EList.DbDataProvider.Extensions
{
    public static class QueryExtensions
    {
        public static IQueryable<T> ToPagedQuery<T>(this IQueryable<T> query, int? pageIndex, int? pageSize)
        {
            if (pageIndex!= null && pageSize!= null)
                return query.Skip(pageIndex.Value * pageSize.Value).Take(pageSize.Value );
            return query;
        }
    }
}
