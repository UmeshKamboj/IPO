using IPOClient.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Entities
{
    public class IPO_PaymentTransaction
    {
        [Key]
        public int PaymentId { get; set; }

        public int GroupId { get; set; }
        public int IpoId { get; set; }

        public int AmountType { get; set; }   // Enum use

        [Column(TypeName = "decimal(18, 4)")]
        public decimal Amount { get; set; }

        public string? Remark { get; set; }

        public DateTime TransactionDate { get; set; }

        public bool IsJVTransaction { get; set; } = false;

        public int CompanyId { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        //  Navigation Properties
        [ForeignKey(nameof(GroupId))]
        public IPO_GroupMaster? Group { get; set; }

        [ForeignKey(nameof(IpoId))]
        public IPO_IPOMaster? Ipo { get; set; }

    }
}
