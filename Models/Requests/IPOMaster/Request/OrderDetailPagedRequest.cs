namespace IPOClient.Models.Requests.IPOMaster.Request
{
    /// <summary>
    /// Order detail request with pagination support
    /// </summary>
    public class OrderDetailPagedRequest
    {
        public int? GroupId { get; set; }
        public int? OrderCategoryId { get; set; }
        public int? InvestorTypeId { get; set; } 
        public string SearchValue { get; set; } = string.Empty;
        public int Skip { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }
}
