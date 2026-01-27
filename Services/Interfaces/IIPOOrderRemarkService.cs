
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOOrderRemarkService
    {
        Task<ReturnData<OrderRemarkResponse>> CreateOrderRemarkAsync(CreateOrderRemarkRequest request, int userId, int companyId);
        Task<ReturnData<List<OrderRemarkDTOResponse>>> GetOrderRemarkDTOByCompanyAsync(int companyId, int? ipoId); //get ipo remarks by ipo id and company id
    }
}
