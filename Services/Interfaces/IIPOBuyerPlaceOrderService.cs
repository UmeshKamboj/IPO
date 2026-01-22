using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOBuyerPlaceOrderService
    {
        Task<ReturnData<BuyerPlaceOrderResponse>> CreateIPOBuyerPlaceOrderAsync(IPOBuyerPlaceOrderRequest request, int createdByUserId, int companyId);
        Task<ReturnData<List<BuyerOrderResponse>>> GetTopFivePlaceOrderListAsync(int ipoId,int companyId);
        Task<ReturnData<List<BuyerOrderResponse>>> GetOrderPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId);
        Task<ReturnData<List<BuyerOrderResponse>>> GetOrderDetailPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId,int orderType);
        Task<ReturnData> UpdateOrderDetailsAsync(UpdateOrderDetailsListRequest request, int modifiedByUserId);
    }
}
