using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Entities
{
    public class IPO_Analysis
    {
        [Key]
        public int Id { get; set; }

        public int IPOId { get; set; }

        [ForeignKey(nameof(IPOId))]
        public IPO_IPOMaster? IPOMaster { get; set; }

        /// <summary>
        /// 1 = Initial P&L Analysis, 2 = After Allotment P&L, 3 = After Listing
        /// </summary>
        public int AnalysisType { get; set; }

        // Tab 1 inputs - Expected Applications
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ExpectedApplications_Retail { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ExpectedApplications_SHNI { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ExpectedApplications_BHNI { get; set; }

        // Tab 2 inputs - Actual Allotted Qty
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ActualAllottedQty_Total { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ActualAllottedQty_Retail { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ActualAllottedQty_SHNI { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ActualAllottedQty_BHNI { get; set; }

        // Shared inputs
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ProfitMargin { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? SpotPremium { get; set; }

        // Tab 3 input
        [Column(TypeName = "decimal(18,4)")]
        public decimal? SpotPrice { get; set; }

        // Stored calculated results as JSON
        public string? CalculatedResultJson { get; set; }

        // Audit Fields
        public int? CreatedBy { get; set; }
        public int? CompanyId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
