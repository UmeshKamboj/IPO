using System.ComponentModel.DataAnnotations;

namespace IPOClient.Models.Requests.IPOMaster.Request
{
    /// <summary>
    /// Simple order list request with group filter only (no pagination/search)
    /// </summary>
    public class OrderListRequest
    {
        public int? GroupId { get; set; }
    }

    /// <summary>
    /// Order detail list request with global search and pagination
    /// </summary>
    public class OrderDetailFilterRequest
    {
        public int? GroupId { get; set; }
        public int? OrderCategoryId { get; set; }
        public int? InvestorTypeId { get; set; }
        public string? SearchValue { get; set; }
        public int Skip { get; set; } = 0;
        public int PageSize { get; set; } = 10;

        // Global search will search across: PANNumber, ClientName, DematNumber, ApplicationNo
    }


    public class CreateOrderDetailRequest : IPOClient.Models.Requests.PaginationRequest
    {
        public int? GroupId { get; set; }
        public int? OrderCategoryId { get; set; }
        public int? InvestorTypeId { get; set; }
        public string? SearchValue { get; set; }

        // Global search will search across: PANNumber, ClientName, DematNumber, ApplicationNo
    }
}
