using IPOClient.Models.Responses;

namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class BuyerPlaceOrderResponse
    {
        public int BuyerMasterId { get; set; }
        public int IPOId { get; set; }
        public int GroupId { get; set; }
        public List<BuyerOrderResponse> Orders { get; set; }

    }
    public class BuyerOrderResponse
    {
        public int OrderId { get; set; }
        public int OrderType { get; set; }
        public int OrderCategory { get; set; }
        public int InvestorType { get; set; }
        public string? PremiumStrikePrice { get; set; }
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public DateTime DateTime { get; set; }

        // display data
        public int? SrNo { get; set; }
        public int? BuyerMasterId { get; set; }
        public int? POChildId { get; set; } //sub order child id
        public string? GroupName { get; set; }
        public int? GroupId { get; set; }
        public string? OrderTypeName { get; set; }
        public string? OrderCategoryName { get; set; }
        public string? InvestorTypeName { get; set; }
        public string? PanNumber { get; set; }
        public string? ClientName { get; set; }
        public int? AllotedQty { get; set; }
        public string? DematNumber { get; set; }
        public string? ApplicationNumber { get; set; }
        public string? Remark { get; set; }
        public decimal? PreOpenPrice { get; set; }

    }
    
}
