using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOBuyerPlaceOrderRepository:IRepository<IPO_BuyerPlaceOrderMaster>
    {
        Task<int> CreateAsync(IPOBuyerPlaceOrderRequest request, int userId, int companyId);
        Task<IPO_BuyerPlaceOrderMaster?> GetByIdAsync(int id, int companyId); // get place order data by master id
        Task<List<IPO_BuyerOrder>> GetTopFivePlaceOrderListAsync(int ipoId,int companyId);
        Task<IPO_BuyerOrder> GetPlaceOrderDataByIdAsync(int orderId,int companyId); //get place order data by order id
        Task<PagedResult<IPO_BuyerOrder>> GetOrderPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId);

        Task<PagedResult<IPO_PlaceOrderChild>> GetOrderDetailPagedListAsync( OrderDetailFilterRequest request, int companyId,int ipoId, int orderType);
        Task<bool> UpdateOrderDetailsAsync(UpdateOrderDetailsListRequest request, int userId);
        Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(OrderStatusFilterRequest request, int companyId);
    }
}
