using IPOClient.Models.Entities;
using IPOClient.Models.Requests;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using Microsoft.AspNetCore.Identity.Data;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOService
    {
        Task<ReturnData<PagedResult<CreateIPOResponse>>> GetAllIPOsAsync(IPOFilterRequest request, int companyId);
        Task<ReturnData<CreateIPOResponse>> GetIPOByIdAsync(int id, int companyId);
        Task<ReturnData<CreateIPOResponse>> CreateIPOAsync(CreateIPORequest request, int createdByUserId, int companyId);
        Task<ReturnData> UpdateIPOAsync(int id, CreateIPORequest request, int modifiedByUserId);
        Task<ReturnData> DeleteIPOAsync(int id, int modifiedByUserId);
        Task<ReturnData<List<IPONameIdResponse>>> GetIPONameIdByCompanyAsync(int companyId);

        Task<ReturnData> UpdateIPOOpenPriceAsync(int ipoId, decimal openPrice, int userId);


    }
}
