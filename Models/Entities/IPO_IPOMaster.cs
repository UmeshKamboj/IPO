using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Entities
{
    public class IPO_IPOMaster
    {
        [Key]
        public int Id { get; set; } 
        public string? IPOName { get; set; } 
        public int? IPOType { get; set; } = 1;
        [Column(TypeName = "decimal(18, 4)")]
        public decimal IPO_Upper_Price_Band { get; set; }
        [Column(TypeName = "decimal(18, 4)")]
        public decimal? OpenIPOPrice { get; set; }
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Total_IPO_Size_Cr { get; set; }
        [Column(TypeName = "decimal(18, 4)")]
        public decimal IPO_Retail_Lot_Size { get; set; }
        [Column(TypeName = "decimal(18, 4)")]
        public decimal? IPO_SHNI_Lot_Size { get; set; }
        [Column(TypeName = "decimal(18, 4)")]
        public decimal? IPO_BHNI_Lot_Size { get; set; } 
        public int Retail_Percentage { get; set; }
        public int? BHNI_Percentage { get; set; }  
        public int? SHNI_Percentage { get; set; }
        public string? Remark { get; set; } 
        // Audit Fields 
        public int? CreatedBy { get; set; }
        public int? CompanyId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; 
        public string? ModifiedBy { get; set; } 
        public DateTime? ModifiedDate { get; set; } 
        public bool IsActive { get; set; } = true; 
        // Navigation property (optional)
        [ForeignKey("IPOType")]
        public IPO_TypeMaster? IPOTypeMaster { get; set; }
    }
}
