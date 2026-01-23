using System.ComponentModel.DataAnnotations;

namespace IPOClient.Models.Entities
{
    public class IPO_Order_Remark
    {
        [Key]
        public int RemarkId { get; set; }

        public int IPOId { get; set; }   // IPO key

        public string Remark { get; set; }

        public int CompanyId { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
