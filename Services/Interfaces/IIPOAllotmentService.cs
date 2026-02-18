using IPOClient.Models.Requests;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOAllotmentService
    {
        /// <summary>
        /// Get list of IPO companies from a registrar
        /// </summary>
        Task<ReturnData<List<IPOAllotmentCompany>>> GetIPOsByRegistrarAsync(string registrar);

        /// <summary>
        /// Check allotment status for a single PAN number
        /// </summary>
        Task<ReturnData<IPOAllotmentResult>> CheckAllotmentAsync(string registrar, string companyCode, string panNumber);

        /// <summary>
        /// Bulk allotment check: fetches all PANs for an IPO, checks allotment on registrar, updates DB
        /// </summary>
        Task<ReturnData<BulkAllotmentCheckResponse>> BulkAllotmentCheckAsync(BulkAllotmentCheckRequest request, int companyId);

        /// <summary>
        /// Firm allotment: mark all order children for an IPO as allotted with their existing quantity
        /// </summary>
        Task<ReturnData<BulkAllotmentCheckResponse>> FirmAllotmentAsync(FirmAllotmentRequest request, int companyId);

        /// <summary>
        /// Get all IPOs from all registrars in parallel (unified list with registrar tag)
        /// </summary>
        Task<ReturnData<List<IPOAllotmentCompany>>> GetAllIPOsAsync();

        /// <summary>
        /// Get current/upcoming IPOs from NSE
        /// </summary>
        Task<ReturnData<List<IPOAllotmentCompany>>> GetIPOsFromNSEAsync();
    }
}
