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

    /// <summary>
    /// Request to update PreOpen Price for IPO
    /// </summary>
    public class UpdatePreOpenPriceRequest
    {
        /// <summary>
        /// IPO Id (required)
        /// </summary>
        public int? IPOId { get; set; }

        /// <summary>
        /// New PreOpen Price value
        /// </summary>
        public decimal PreOpenPrice { get; set; }

        /// <summary>
        /// Optional: Specific OrderId to update all children of that order.
        /// </summary>
        //public int? OrderId { get; set; }

        /// <summary>
        /// Optional: Specific POChildId to update single child item.
        /// </summary>
        public int? POChildId { get; set; }

        // Priority: POChildId > OrderId > IPOId (all orders)
    }
}
