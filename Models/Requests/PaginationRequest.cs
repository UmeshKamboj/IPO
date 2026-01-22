namespace IPOClient.Models.Requests
{
    /// <summary>
    /// Base pagination request with global search support
    /// </summary>
    public class PaginationRequest
    {
        /// <summary>
        /// Global search term - searches across multiple fields
        /// </summary>
        public string SearchValue { get; set; } = string.Empty;

        /// <summary>
        /// Number of records to skip (offset-based pagination)
        /// </summary>
        public int Skip { get; set; } = 0;

        /// <summary>
        /// Number of records per page (default: 10)
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
