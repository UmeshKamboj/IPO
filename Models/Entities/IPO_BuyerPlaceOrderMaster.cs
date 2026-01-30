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
        public ICollection<IPO_BuyerOrder> Orders { get; set; }
        // Audit Fields 
        public int? CreatedBy { get; set; }
        public int? CompanyId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
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
        public bool ApplicateRate { get; set; } = false; // If true: Premium, If false: Application
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

        public string? Remarks { get; set; } //comma separated remark IDs

        public bool IsDeleted { get; set; } = false;

        public int OrderSource { get; set; } = 1; // Source of the order (Manual and Upload)

    }
    public class IPO_PlaceOrderChild
    {
        [Key]
        public int POChildId { get; set; } //Place Order Child Id

        [ForeignKey(nameof(IPO_BuyerOrder))]
        public int OrderId { get; set; }
        public IPO_BuyerOrder IPOOrder { get; set; }

        public int Quantity { get; set; } = 1;
        public int GroupId { get; set; } // Added GroupId for easier querying

        // Navigation to Group
        [ForeignKey(nameof(GroupId))]
        public IPO_GroupMaster? Group { get; set; }

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
        public bool IsDeleted { get; set; } = false;
    }


    //deleted all order entities
    public class IPO_DeleteOrderHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public DateTime DeletedDate { get; set; } = DateTime.UtcNow;
        public int DeletedBy { get; set; }
        public int CompanyId { get; set; }

        public int TotalOrdersDeleted { get; set; }
        public string? Remark { get; set; }

        //  Navigation (what was deleted)
        public ICollection<OrderMaster_DeletedHistory> DeletedMasters { get; set; }
        public ICollection<Order_DeletedHistory> DeletedOrders { get; set; }
        public ICollection<OrderChild_DeletedHistory> DeletedChildren { get; set; }
    }
    public class OrderMaster_DeletedHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int BuyerMasterId { get; set; }
        public int IPOId { get; set; }

        public int DeleteHistoryId { get; set; }
        [ForeignKey(nameof(DeleteHistoryId))]
        public IPO_DeleteOrderHistory DeleteHistory { get; set; }

        public int? CreatedBy { get; set; }
        public int? CompanyId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; } = DateTime.UtcNow;
    }
    public class Order_DeletedHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int OrderId { get; set; }
        public int BuyerMasterId { get; set; }

        public int DeleteHistoryId { get; set; }
        [ForeignKey(nameof(DeleteHistoryId))]
        public IPO_DeleteOrderHistory DeleteHistory { get; set; }

        public int OrderType { get; set; }
        public int OrderCategory { get; set; }
        public int InvestorType { get; set; }

        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public DateTime DateTime { get; set; }

        public string? Remarks { get; set; }

        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; } = DateTime.UtcNow;
    }
    public class OrderChild_DeletedHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int POChildId { get; set; }
        public int OrderId { get; set; }

        public int DeleteHistoryId { get; set; }
        [ForeignKey(nameof(DeleteHistoryId))]
        public IPO_DeleteOrderHistory DeleteHistory { get; set; }
        public int Quantity { get; set; } = 1;
        public int GroupId { get; set; }
        public string? PANNumber { get; set; }
        public string? ClientName { get; set; }
        public int? AllotedQty { get; set; }
        public string? DematNumber { get; set; }
        public string? ApplicationNo { get; set; }

        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; } = DateTime.UtcNow;
    }

}
