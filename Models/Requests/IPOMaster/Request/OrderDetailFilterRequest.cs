using System.ComponentModel.DataAnnotations;

namespace IPOClient.Models.Requests.IPOMaster.Request
{
    /// <summary>
    /// Order list request with global search and pagination
    /// </summary>
    public class OrderDetailFilterRequest : IPOClient.Models.Requests.PaginationRequest
    {
   

        public int? GroupId { get; set; }
        public int? OrderCategoryId { get; set; }
        public int? InvestorTypeId { get; set; }

        // Global search will search across: PANNumber, ClientName, DematNumber, ApplicationNo
    }
}
