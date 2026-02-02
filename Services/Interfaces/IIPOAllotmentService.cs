using IPOClient.Models.Requests;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOAllotmentService
    {
        /// <summary>
        /// Get list of IPO companies from a registrar
        /// </summary>
        Task<ApiResponse<List<IPOAllotmentCompany>>> GetIPOsByRegistrarAsync(string registrar);

        /// <summary>
        /// Check allotment status for a single PAN number
        /// </summary>
        Task<ApiResponse<IPOAllotmentResult>> CheckAllotmentAsync(string registrar, string companyCode, string panNumber);

        /// <summary>
        /// Bulk allotment check: fetches all PANs for an IPO, checks allotment on registrar, updates DB
        /// </summary>
        Task<ApiResponse<BulkAllotmentCheckResponse>> BulkAllotmentCheckAsync(BulkAllotmentCheckRequest request, int companyId);

        /// <summary>
        /// Firm allotment: mark all order children for an IPO as allotted with their existing quantity
        /// </summary>
        Task<ApiResponse<BulkAllotmentCheckResponse>> FirmAllotmentAsync(int ipoId, int companyId);
    }
}
