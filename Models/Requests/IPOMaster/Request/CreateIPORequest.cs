using IPOClient.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class CreateIPORequest
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "IPO Name is required")]
        [MaxLength(50, ErrorMessage = "IPO Name cannot exceed 50 characters")]
        public string? IPOName { get; set; }
        public int  IPOType { get; set; } = 1;

        [Required(ErrorMessage = "IPO Upper Price Band is required")]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal IPO_Upper_Price_Band { get; set; }

        [Required(ErrorMessage = "Total IPO Size in Cr. is required")]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Total_IPO_Size_Cr { get; set; }

        [Required(ErrorMessage = "Retail Lot Size is required")]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal IPO_Retail_Lot_Size { get; set; }

       
        [Column(TypeName = "decimal(18, 4)")]   
        public decimal? IPO_SHNI_Lot_Size { get; set; }
        
        [Column(TypeName = "decimal(18, 4)")]       
        public decimal? IPO_BHNI_Lot_Size { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Retail Percentage must be between 0 and 100")]
        public int Retail_Percentage { get; set; }


        [Range(0, 100, ErrorMessage = "BHNI Percentage must be between 0 and 100")]
        public int? BHNI_Percentage { get; set; }


        [Range(0, 100, ErrorMessage = "SHNI Percentage must be between 0 and 100")]
        public int? SHNI_Percentage { get; set; }
        public string? Remark { get; set; }

        // Audit Fields 
        
    }
}
