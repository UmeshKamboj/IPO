using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOBuyerPlaceOrderRepository:IRepository<IPO_BuyerPlaceOrderMaster>
    {
        Task<int> CreateAsync(IPOBuyerPlaceOrderRequest request, int userId, int companyId);
        Task<IPO_BuyerPlaceOrderMaster?> GetByIdAsync(int id, int companyId);
        Task<List<IPO_BuyerOrder>> GetTopFivePlaceOrderListAsync(int ipoId,int companyId);
        Task<IPO_BuyerOrder> GetPlaceOrderDataByIdAsync(int orderId,int companyId);
        Task<PagedResult<IPO_BuyerOrder>> GetOrderPagedListAsync(OrderDetailPagedRequest request, int companyId, int ipoId);
        Task<List<IPO_BuyerOrder>> GetOrderListAsync(OrderListRequest request, int companyId, int ipoId);
        Task<PagedResult<IPO_PlaceOrderChild>> GetOrderDetailPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType);
        Task<bool> UpdateOrderDetailsAsync(UpdateOrderDetailsListRequest request, int userId); //order child's update
        Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(OrderStatusFilterRequest request, int companyId);
        Task<PagedResult<IPO_PlaceOrderChild>> GetAllOrderChildrenWithSearchAsync(OrderDetailPagedRequest request, int companyId, int ipoId);

        Task<int> UpdateOrderAsync(EditIPOOrderRequest request, int userId); //order update

        Task<bool> DeleteOrderAsync(int orderId, int userId);  //Delete single order

        Task<bool> BulkOrderUploadAsync(int ipoId, List<string[]> rows, int createdByUserId, int companyId);//Bulk Order Upload

        Task<byte[]?> DeletedAllOrderAsync(int ipoId, int userId, int companyId); //Delete all order data behlaf of IPO

        Task<List<IPO_PlaceOrderChild>> GetOrdersAsync( int ipoId,int companyId, DownloadFilterType downloadFilterType);
        Task<string> ResolveRemarkNamesAsync(string? remarkIds, int ipoId,int companyId);

        Task<PagedResult<IPO_PlaceOrderChild>> GetClientWisePagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId);
        Task<List<IPO_PlaceOrderChild>> GetGroupWiseBillingListAsync(GroupWiseBillingRequest request, int companyId, int ipoId);

        Task<PagedResult<IPO_PlaceOrderChild>> GetOrderDetailPagedListByOrderIdAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType,int orderId);
    }
}
