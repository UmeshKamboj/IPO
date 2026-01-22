using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace IPOClient.Models.Entities
{
    public class IPO_BuyerPlaceOrderMaster
    {
        [Key]
        public int BuyerMasterId { get; set; }
        public int IPOId { get; set; } 
        public int GroupId { get; set; }
        // Navigation to Group
        [ForeignKey(nameof(GroupId))]
        public IPO_GroupMaster? Group { get; set; }
        public ICollection<IPO_BuyerOrder> Orders { get; set; }
        // Audit Fields 
        public int? CreatedBy { get; set; }
        public int? CompanyId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class IPO_BuyerOrder
    {
        [Key]
        public int OrderId { get; set; }
        [ForeignKey(nameof(IPO_BuyerPlaceOrderMaster))]
        public int BuyerMasterId { get; set; }

        // Navigation property
        public IPO_BuyerPlaceOrderMaster BuyerMaster { get; set; }

        public int OrderType { get; set; }  // BUY / SELL
        public int OrderCategory { get; set; }  // Retail / SHNI / BHNI / Premium / Call / Put / Subject To
        public int InvestorType { get; set; }  // OPTIONS / PREMIUM / BHNI / SHNI
        public string? PremiumStrikePrice { get; set; } // nullable for non-options
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        // Audit Fields 
        public int? CreatedBy { get; set; }
        public int? CompanyId { get; set; }
        public DateTime OrderCreatedDate { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public ICollection<IPO_PlaceOrderChild> OrderChild { get; set; } //order child items

    }
    public class IPO_PlaceOrderChild
    {
        [Key]
        public int POChildId { get; set; } //Place Order Child Id

        [ForeignKey(nameof(IPO_BuyerOrder))]
        public int OrderId { get; set; }
        public IPO_BuyerOrder IPOOrder { get; set; }

        public int Quantity { get; set; } = 1;

        public string? PANNumber { get; set; }
        public string? ClientName { get; set; }
        public int? AllotedQty { get; set; }
        public string? DematNumber { get; set; }
        public string? ApplicationNo { get; set; }
        public int? CreatedBy { get; set; }
        public int? CompanyId { get; set; }
        public DateTime ChildOrderCreatedDate { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
