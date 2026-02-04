using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOBuyerPlaceOrderService
    {
        Task<ReturnData<BuyerPlaceOrderResponse>> CreateIPOBuyerPlaceOrderAsync(IPOBuyerPlaceOrderRequest request, int createdByUserId, int companyId);
        Task<ReturnData<BuyerPlaceOrderResponse>> GetPlaceOrderByIdAsync(int masterId, int companyId);
        Task<ReturnData<List<BuyerOrderResponse>>> GetTopFivePlaceOrderListAsync(int ipoId,int companyId);
        Task<ReturnData<BuyerOrderResponse>> GetPlaceOrderDataByIdAsync(int orderId, int companyId);
        Task<ReturnData<List<BuyerOrderResponse>>> GetOrderListAsync(OrderListRequest request, int companyId, int ipoId);
        Task<ReturnData<PagedResult<BuyerOrderResponse>>> GetOrderDetailPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId,int orderType);
        Task<ReturnData> UpdateOrderDetailsAsync(UpdateOrderDetailsListRequest request, int modifiedByUserId); //order child's update
        Task<ReturnData<OrderStatusSummaryResponse>> GetOrderStatusSummaryAsync(OrderStatusFilterRequest request, int companyId);
        Task<ReturnData<PagedResult<BuyerOrderResponse>>> GetAllOrderChildrenWithSearchAsync(OrderDetailPagedRequest request, int companyId, int ipoId);
        Task<ReturnData> UpdateOrderAsync(EditIPOOrderRequest request, int modifiedByUserId); //update order
        Task<ReturnData> DeleteOrderAsync(int orderId, int userId); //Soft delete Order
        Task<ReturnData<FileResponse>> DeleteAllOrderAsync(int ipoId, int userId, int companyId); //Soft delete all Order and backup
        Task<ReturnData> BulkOrderUploadAsync(int ipoId, IFormFile file, int createdByUserId, int companyId, int? orderId);//Bulk Order Upload

        Task<ReturnData<FileResponse>> DownloadSingleFileAsync(int ipoId,int companyId,DownloadFilterType downloadFilterType);
        Task<ReturnData<FileResponse>> DownloadGroupWiseFileAsync(int ipoId,int companyId,DownloadFilterType downloadFilterType);

        Task<ReturnData<ClientWiseBillingResponse>> GetClientWiseBillingPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId);
        Task<ReturnData> UpdatePreOpenPriceAsync(UpdatePreOpenPriceRequest request, int companyId, int userId);
        Task<ReturnData> SyncChildrenPreOpenPriceFromParentAsync(int ipoId, int companyId, int userId);
        OrderCategoryOptionsResponse GetOrderCategoryOptions(int orderCategoryType);
        Task<ReturnData<GroupWiseBillingPagedResponse>> GetGroupWiseBillingListAsync(GroupWiseBillingRequest request, int companyId, int ipoId);
        Task<ReturnData<PagedResult<BuyerOrderResponse>>> GetOrderDetailPagedListByOrderIdAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType,int orderId);

        // Download methods for billing
        Task<ReturnData<FileResponse>> DownloadGroupWiseBillingExcelAsync(GroupWiseBillingRequest request, int companyId, int ipoId);
        Task<ReturnData<FileResponse>> DownloadClientWiseBillingExcelAsync(OrderDetailFilterRequest request, int companyId, int ipoId);
        Task<ReturnData<FileResponse>> DownloadClientWiseBillingPdfAsync(OrderDetailFilterRequest request, int companyId, int ipoId);
    }
}
