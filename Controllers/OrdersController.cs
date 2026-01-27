using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Services.Implementations;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IIPOBuyerPlaceOrderService _ipoBuyerPlaceOrderService;

        public OrdersController(IIPOBuyerPlaceOrderService ipoBuyerPlaceOrderService)
        {
            _ipoBuyerPlaceOrderService = ipoBuyerPlaceOrderService;
        }

        /// <summary>
        /// IPO Buy/Sell Place order
        /// </summary>
        [HttpPost("buyplaceorder")]
        public async Task<IActionResult> IPOBuyPlaceOrder([FromBody] IPOBuyerPlaceOrderRequest request)
        {
            var userId = GetCurrentUserId();
            var companyId = GetCompanyId();

            var result = await _ipoBuyerPlaceOrderService.CreateIPOBuyerPlaceOrderAsync(request, userId, companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Get place order by OrderId
        /// </summary>
        [HttpGet("getorder/{orderId}")]
        public async Task<IActionResult> GetPlaceOrderByOrderId(int orderId)
        {
            var companyId = GetCompanyId();
            var result = await _ipoBuyerPlaceOrderService.GetPlaceOrderDataByIdAsync(orderId, companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Get top 5 place orders for an IPO
        /// </summary>
        [HttpGet("gettopfiveplaceorder/{ipoId}")]
        public async Task<IActionResult> GetTopFiveOrderPlaced(int ipoId)
        {
            var companyId = GetCompanyId();
            var result = await _ipoBuyerPlaceOrderService.GetTopFivePlaceOrderListAsync(ipoId, companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Get buy order details with global search and pagination
        /// </summary>
        /// <remarks>
        /// Returns paginated list of buy orders with optional global search across PANNumber, ClientName, DematNumber, ApplicationNo.
        /// Filters: GroupId, OrderCategoryId, InvestorTypeId
        /// Pagination: skip, pageSize
        /// </remarks>
        [HttpPost("{ipoId}/orderdetail/buy")]
        public async Task<IActionResult> GetBuyOrderDetailList([FromBody] OrderDetailFilterRequest request, int ipoId)
        {
            var companyId = GetCompanyId();
            int buyValue = (int)IPOOrderType.BUY;
            var result = await _ipoBuyerPlaceOrderService.GetOrderDetailPagedListAsync(request, companyId, ipoId, buyValue);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Get sell order details with global search and pagination
        /// </summary>
        /// <remarks>
        /// Returns paginated list of sell orders with optional global search across PANNumber, ClientName, DematNumber, ApplicationNo.
        /// Filters: GroupId, OrderCategoryId, InvestorTypeId
        /// Pagination: skip, pageSize
        /// </remarks>
        [HttpPost("{ipoId}/orderdetail/sell")]
        public async Task<IActionResult> GetSellOrderDetailList([FromBody] OrderDetailFilterRequest request, int ipoId)
        {
            var companyId = GetCompanyId();
            int sellValue = (int)IPOOrderType.SELL;
            var result = await _ipoBuyerPlaceOrderService.GetOrderDetailPagedListAsync(request, companyId, ipoId, sellValue);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Get all orders without pagination (simple list with group filter only)
        /// </summary>
        [HttpPost("{ipoId}/order/list")]
        public async Task<IActionResult> GetOrderList(int ipoId, [FromBody] OrderListRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _ipoBuyerPlaceOrderService.GetOrderListAsync(request, companyId, ipoId);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Update Bulk Order Details
        /// </summary>
        [HttpPut("update/orderdetails")]
        public async Task<IActionResult> UpdateBulkOrderDetails([FromBody] UpdateOrderDetailsListRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _ipoBuyerPlaceOrderService.UpdateOrderDetailsAsync(request, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
        /// <summary>
        /// Update Order
        /// </summary>
        [HttpPut("update/order")]
        public async Task<IActionResult> UpdateOrder([FromBody] EditIPOOrderRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _ipoBuyerPlaceOrderService.UpdateOrderAsync(request, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
        /// <summary>
        /// Order status popup summary
        /// </summary>
        [HttpPost("orderstatussummary")]
        public async Task<IActionResult> OrderStatusSummary([FromBody] OrderStatusFilterRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _ipoBuyerPlaceOrderService.GetOrderStatusSummaryAsync(request, companyId);
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Get all order children with pagination and global search
        /// </summary>
        /// <remarks>
        /// Returns paginated list of all order children with optional filters.
        /// Filters: GroupId, OrderCategoryId, InvestorTypeId, SearchValue
        /// Pagination: skip, pageSize
        /// </remarks>
        [HttpPost("{ipoId}/orders/all")]
        public async Task<IActionResult> GetAllOrderChildrenWithSearch(int ipoId, [FromBody] OrderDetailPagedRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _ipoBuyerPlaceOrderService.GetAllOrderChildrenWithSearchAsync(request, companyId, ipoId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
        /// <summary>
        /// Delete a order (soft delete) 
        /// </summary>
        [HttpDelete("deleteorder")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var companyId = GetCompanyId();

            var result = await _ipoBuyerPlaceOrderService.DeleteOrderAsync(orderId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Delete all order (soft delete) 
        /// </summary>
        [HttpDelete("deleteallorder")]
        public async Task<IActionResult> DeleteAllOrder(int ipoId)
        {
            var companyId = GetCompanyId();
            int userId = GetCurrentUserId();
            var result = await _ipoBuyerPlaceOrderService.DeleteAllOrderAsync(ipoId,userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Upload IPO bulk orders via CSV
        /// </summary>
        [HttpPost("uploadbulk")]
        public async Task<IActionResult> BulkOrderUploadCsv([FromForm]  int ipoId,IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("CSV file is required");

            if (!file.FileName.EndsWith(".csv"))
                return BadRequest("Only CSV files are allowed");
            int companyId = GetCompanyId();  
            int userId = GetCurrentUserId();
            var result = await _ipoBuyerPlaceOrderService.BulkOrderUploadAsync(ipoId,file, userId,companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }
        // Helpers to get claims
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private int GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("cid")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }
    }
}
