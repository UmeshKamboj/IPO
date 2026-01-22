using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class CreateIPOResponse
    {
        public int? Id { get; set; } 
        public string? IPOName { get; set; }
        public int? IPOType { get; set; } = 1;
        [Column(TypeName = "decimal(18, 4)")]
        public decimal  IPO_Upper_Price_Band { get; set; }
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
        public bool? IsActive { get; set; }
    }
}
