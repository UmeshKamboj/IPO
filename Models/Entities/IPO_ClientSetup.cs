using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Entities
{
    public class IPO_ClientSetup
    {
        [Key]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(10)]
        public string PANNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public IPO_GroupMaster? Group { get; set; }

        [MaxLength(100)]
        public string? ClientDPId { get; set; }

        // Audit Fields
        public int CompanyId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class IPO_ClientDeleteHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public DateTime DeletedDate { get; set; }
        public int DeletedBy { get; set; }
        public int CompanyId { get; set; }
        public int TotalClientsDeleted { get; set; }
        public string? Remark { get; set; }

        // Navigation property to deleted clients
        public ICollection<IPO_ClientDeleteHistoryDetail>? Details { get; set; }
    }

    public class IPO_ClientDeleteHistoryDetail
    {
        [Key]
        public int DetailId { get; set; }

        public int HistoryId { get; set; }

        [ForeignKey(nameof(HistoryId))]
        public IPO_ClientDeleteHistory? History { get; set; }

        public int ClientId { get; set; }
        public string PANNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string? ClientDPId { get; set; }
    }
}
