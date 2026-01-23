using System.ComponentModel.DataAnnotations;

namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class IPOBuyerPlaceOrderRequest
    { 
        public int IPOId { get; set; }

        [Required(ErrorMessage = "Group is required")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Date and time is required")]
        public DateTime DateTime { get; set; }
        public List<BuyerOrderRequest>? Orders { get; set; }
        public string? RemarksIds { get; set; } //comma separated remark IDs


    }
    public class BuyerOrderRequest
    {
        public int OrderType { get; set; }      // BUY / SELL
        public int OrderCategory { get; set; }  // Retail / SHNI / BHNI / Premium / Call / Put / Subject To
        public int InvestorType { get; set; }   // OPTIONS / PREMIUM / BHNI / SHNI
        public string? PremiumStrikePrice { get; set; }
        public bool ApplicateRate { get; set; } = false; // If true: Premium, If false: Application
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
    }

    public class UpdateOrderDetailsListRequest
    {
        public List<UpdateOrderDetailRequest> Orders { get; set; } = new List<UpdateOrderDetailRequest>();
    }

    public class UpdateOrderDetailRequest
    { 
        public int POChildId { get; set; } //Place Order Sub Child Id
        public string? PANNumber { get; set; } = string.Empty;
        public string? ClientName { get; set; } = string.Empty;
        public string? DematNumber { get; set; } = string.Empty;
        public string? ApplicationNumber { get; set; } = string.Empty;
        public int? AllotedQty { get; set; } =null;
    }

}
