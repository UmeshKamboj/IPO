using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;

namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOAnalysisRepository
    {
        Task<IPO_IPOMaster?> GetIPOMasterAsync(int ipoId, int companyId);
        Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(int ipoId, int companyId);
        Task<ShareQtyData> GetShareQtyDataAsync(int ipoId, int companyId);
        Task<ShareQtyData> GetAllotedShareQtyDataAsync(int ipoId, int companyId);
        Task<IPO_Analysis?> GetAnalysisAsync(int ipoId, int analysisType, int companyId);
        Task<List<IPO_Analysis>> GetAllAnalysesAsync(int ipoId, int companyId);
        Task<int> UpsertAnalysisAsync(IPO_Analysis analysis);
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
