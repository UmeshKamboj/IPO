namespace IPOClient.Models.Responses
{
    public class PagedResult<T>
    {
        public List<T>? Items { get; set; }
        public int TotalCount { get; set; }
        public int Skip { get; set; }
        public int PageSize { get; set; }

        // Calculated properties
        public int CurrentPage => Skip / PageSize + 1;
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
        public bool HasPreviousPage => Skip > 0;
        public bool HasNextPage => Skip + PageSize < TotalCount;

        // EXTRA OPTIONAL FIELDS (NOT MANDATORY)
        public Dictionary<string, int>? Extras { get; set; }

        public PagedResult()
        {
            Items = new List<T>();
        }

        public PagedResult(List<T> items, int totalCount, int skip, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Skip = skip;
            PageSize = pageSize;
        }

        // Backward compatibility constructor (converts pageNumber to skip)
        [Obsolete("Use constructor with skip parameter instead")]
        public static PagedResult<T> FromPageNumber(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            int skip = (pageNumber - 1) * pageSize;
            return new PagedResult<T>(items, totalCount, skip, pageSize);
        }
    }
}
