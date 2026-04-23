using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;

namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOAnalysisRepository
    {
        Task<IPO_IPOMaster?> GetIPOMasterAsync(int ipoId, int companyId);
        Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(int ipoId, int companyId, decimal? overridePreOpenPrice = null);
        Task<ShareQtyData> GetShareQtyDataAsync(int ipoId, int companyId);
        Task<ShareQtyData> GetAllotedShareQtyDataAsync(int ipoId, int companyId);
        Task<IPO_Analysis?> GetAnalysisAsync(int ipoId, int analysisType, int companyId);
        Task<List<IPO_Analysis>> GetAllAnalysesAsync(int ipoId, int companyId);
        Task<int> UpsertAnalysisAsync(IPO_Analysis analysis);
        Task<ActualAllottedQtySummary> GetActualAllottedQtySummaryAsync(int ipoId, int companyId);
        Task<SharedAnalysisFields> GetLatestSharedFieldsAsync(int ipoId, int companyId);
        Task UpdateSharedFieldsAsync(int ipoId, int companyId, decimal? profitMargin, decimal? spotPremium, decimal? spotPrice);
    }

    /// <summary>
    /// Shared fields (ProfitMargin, SpotPremium, SpotPrice) common across all analysis tabs
    /// </summary>
    public class SharedAnalysisFields
    {
        public decimal? ProfitMargin { get; set; }
        public decimal? SpotPremium { get; set; }
        public decimal? SpotPrice { get; set; }
    }

    public class ActualAllottedQtySummary
    {
        public decimal Total { get; set; }
        public decimal Retail { get; set; }
        public decimal SHNI { get; set; }
        public decimal BHNI { get; set; }
    }

    /// <summary>
    /// Holds grouped share qty data: SUM(child.Quantity) grouped by OrderCategory, InvestorType, OrderType
    /// </summary>
    public class ShareQtyData
    {
        public List<ShareQtyRow> Rows { get; set; } = new();
    }

    public class ShareQtyRow
    {
        public int OrderCategory { get; set; }
        public int InvestorType { get; set; }
        public int OrderType { get; set; }
        public int TotalQty { get; set; }
    }
}
