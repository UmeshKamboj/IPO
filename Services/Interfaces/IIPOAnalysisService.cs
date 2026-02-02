using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOAnalysisService
    {
        Task<ReturnData<IPOAnalysisResponse>> CalculateAnalysisAsync(IPOAnalysisRequest request, int companyId);
        Task<ReturnData<IPOAnalysisResponse>> SubmitAnalysisAsync(IPOAnalysisRequest request, int userId, int companyId);
        Task<ReturnData<IPOAnalysisResponse>> GetAnalysisAsync(int ipoId, int analysisType, int companyId);
        Task<ReturnData<List<IPOAnalysisResponse>>> GetAllAnalysesAsync(int ipoId, int companyId);
    }
}
