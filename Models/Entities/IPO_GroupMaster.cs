using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Entities
{
    public class IPO_GroupMaster
    {
        [Key]
        public int IPOGroupId { get; set; }
        public string? GroupName { get; set; }
        public string? MobileNo { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Remark { get; set; }

        public int CompanyId { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public int? IPOId { get; set; }

        [ForeignKey(nameof(IPOId))]
        public IPO_IPOMaster? IPOMaster { get; set; }
    }
}
