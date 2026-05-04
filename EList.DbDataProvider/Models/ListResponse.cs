namespace EList.DbDataProvider.Models
{
    public class ListResponse<T>
    {
        public int TotalCount { get; set; }
        public List<T> Items { get; set; }

        public ListResponse(int totalCount, List<T> items) 
        {
            TotalCount = totalCount;
            Items = items;
        }
    }

    public class ValuedListResponse<T> : ListResponse<T>
    {
        public double? Value { get; set; }
        public ValuedListResponse(int totalCount, double? value, List<T> items ) : base(totalCount, items)
        {
            Value = value;
        }
    }
}
