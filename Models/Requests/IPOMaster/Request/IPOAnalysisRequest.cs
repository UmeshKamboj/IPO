namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class IPOAnalysisRequest
    {
        public int IPOId { get; set; }

        /// <summary>
        /// 1 = Initial P&L Analysis, 2 = After Allotment P&L, 3 = After Listing
        /// </summary>
        public int AnalysisType { get; set; }

        // Tab 1 - Expected Applications
        public decimal? ExpectedApplications_Retail { get; set; }
        public decimal? ExpectedApplications_SHNI { get; set; }
        public decimal? ExpectedApplications_BHNI { get; set; }

        // Tab 2 - Actual Allotted Qty
        public decimal? ActualAllottedQty_Total { get; set; }
        public decimal? ActualAllottedQty_Retail { get; set; }
        public decimal? ActualAllottedQty_SHNI { get; set; }
        public decimal? ActualAllottedQty_BHNI { get; set; }

        // Shared
        public decimal? ProfitMargin { get; set; }
        public decimal? SpotPremium { get; set; }

        // Tab 3
        public decimal? SpotPrice { get; set; }
    }
}
