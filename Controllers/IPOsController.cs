using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/ipos")]
    [Authorize]
    public class IPOsController : ControllerBase
    {
        private readonly IIPOService _ipoService;
        private readonly IIPOGroupService _ipoGroupService;
        private readonly IIPOBuyerPlaceOrderService _ipoBuyerPlaceOrderService;

        public IPOsController(IIPOService ipoService,IIPOGroupService ipoGroupService, IIPOBuyerPlaceOrderService ipoBuyerPlaceOrderService)
        {
            _ipoService = ipoService;
            _ipoGroupService = ipoGroupService;
            _ipoBuyerPlaceOrderService = ipoBuyerPlaceOrderService;
        }

        /// <summary>
        /// Get all IPOs with global search and pagination
        /// </summary>
        /// <remarks>
        /// Returns paginated list of IPOs with optional global search across IPOName, Remark.
        /// Default: skip=0, pageSize=10
        /// </remarks>
        /// <param name="request">Search and paging parameters (searchValue, skip, pageSize)</param>
        /// <returns>Paginated list of IPOs</returns>
        [HttpPost("list")]
        public async Task<IActionResult> GetAllIPOs([FromBody] IPOFilterRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _ipoService.GetAllIPOsAsync(request, companyId);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Get IPO by ID
        /// </summary>
        /// <param name="id">IPO ID</param>
        /// <returns>IPO details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetIPOById(int id)
        {
            var companyId = GetCompanyId();
            var result = await _ipoService.GetIPOByIdAsync(id, companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Create new IPO
        /// </summary>
        /// <param name="request">IPO creation details</param>
        /// <returns>Created IPO</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateIPO([FromBody] CreateIPORequest request)
        {
            var userId = GetCurrentUserId();
            var companyId = GetCompanyId();

            var result = await _ipoService.CreateIPOAsync(request, userId, companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return CreatedAtAction(nameof(GetIPOById), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Update IPO
        /// </summary>
        /// <param name="id">IPO ID to update</param>
        /// <param name="request">Updated IPO details</param>
        [HttpPut("{id}/update")]
        public async Task<IActionResult> UpdateIPO(int id, [FromBody] CreateIPORequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _ipoService.UpdateIPOAsync(id, request, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Delete IPO (soft delete)
        /// </summary>
        /// <param name="id">IPO ID to delete</param>
        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> DeleteIPO(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _ipoService.DeleteIPOAsync(id, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
        /// <summary>
        /// Get IPO name + id list for current company
        /// </summary>
        [HttpGet("getiponames")]
        public async Task<IActionResult> GetIPONamesByCompanyId()
        {
            var companyId = GetCompanyId();
            var result = await _ipoService.GetIPONameIdByCompanyAsync(companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }


        // --------------------------
        // Start IPO Group endpoints
        // --------------------------

        /// <summary>
        /// Create new IPO Group
        /// </summary>
        [HttpPost("groups/create")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateIPOGroupRequest request)
        {
            var userId = GetCurrentUserId();
            var companyId = GetCompanyId();
            var result = await _ipoGroupService.CreateGroupAsync(request, userId, companyId);
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);
            return Ok(result);
        }

        /// <summary>
        /// Update Group
        /// </summary>
        [HttpPut("groups/{id}/update")]
        public async Task<IActionResult> UpdateGroup(int id, [FromBody] CreateIPOGroupRequest request)
        {
            var userId = GetCurrentUserId();
            request.Id = id;
            var result = await _ipoGroupService.UpdateGroupAsync(id, request, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Delete Group (soft delete)
        /// </summary>
        [HttpDelete("groups/{id}/delete")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _ipoGroupService.DeleteGroupAsync(id, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Get list of groups for current company with IPO filter
        /// </summary>
        [HttpGet("groups/{ipoId}")]
        public async Task<IActionResult> GetGroups(int? ipoId)
        {
            var companyId = GetCompanyId();
            var result = await _ipoGroupService.GetGroupsByCompanyAsync(companyId, ipoId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }


        /// <summary>
        /// Get list of groups for current company
        /// </summary>
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var companyId = GetCompanyId();
            var result = await _ipoGroupService.GetGroupsByCompanyAsync(companyId, null);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        // --------------------------
        // END IPO Group endpoints
        // --------------------------




        // --------------------------
        // Start Order endpoints
        // --------------------------
        /// <summary>
        /// IPO Buy Place order
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
        /// Get top 5  place orders for an IPO
        /// </summary>
        [HttpGet("gettopfiveplaceorder/{ipoId}")]
        public async Task<IActionResult> GetTopFiveOrderPlaced(int ipoId)
        {
            var companyId = GetCompanyId();
            var result = await _ipoBuyerPlaceOrderService.GetTopFivePlaceOrderListAsync(ipoId,companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
        /// <summary>
        /// Get all Order detail with global search and pagination
        /// </summary>
        /// <remarks>
        /// Returns paginated list of orders with optional global search across PANNumber, ClientName, DematNumber, ApplicationNo.
        /// Default: skip=0, pageSize=10
        /// Required: moduleName (buy/sell/all)
        /// </remarks>
        /// <returns>Paginated order list</returns>
        [HttpPost("{ipoId}/orderdetail/buy")]
        public async Task<IActionResult> GetBuyOrderDetailList([FromBody] OrderDetailFilterRequest request,int ipoId)
        {
            var companyId = GetCompanyId();
            int buyValue = (int)IPOOrderType.BUY;//BUY Place Order Type
            var result = await _ipoBuyerPlaceOrderService.GetOrderDetailPagedListAsync(request, companyId, ipoId, buyValue);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }
        /// <summary>
        /// Get all Order detail with global search and pagination
        /// </summary>
        /// <remarks>
        /// Returns paginated list of orders with optional global search across PANNumber, ClientName, DematNumber, ApplicationNo.
        /// Default: skip=0, pageSize=10
        /// Required: moduleName (buy/sell/all)
        /// </remarks>
        /// <returns>Paginated order list</returns>
        [HttpPost("{ipoId}/orderdetail/sell")]
        public async Task<IActionResult> GetSellOrderDetailList([FromBody] OrderDetailFilterRequest request, int ipoId)
        {
            var companyId = GetCompanyId();
            int SellValue = (int)IPOOrderType.SELL;//SELL Place Order Type
            var result = await _ipoBuyerPlaceOrderService.GetOrderDetailPagedListAsync(request, companyId, ipoId, SellValue);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Get all Order with global search and pagination (Alias for orderdetail/list)
        /// </summary>
        /// <remarks>
        /// Returns paginated list of orders with optional global search across PANNumber, ClientName, DematNumber, ApplicationNo.
        /// Default: skip=0, pageSize=10
        /// Required: moduleName (buy/sell/all)
        /// </remarks>
        /// <returns>Paginated order list</returns>
        [HttpPost("{ipoId}/order/list")]
        public async Task<IActionResult> GetOrderList([FromBody] OrderDetailFilterRequest request, int ipoId)
        {
            var companyId = GetCompanyId();
            var result = await _ipoBuyerPlaceOrderService.GetOrderPagedListAsync(request, companyId, ipoId);

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
        // --------------------------
        // End Order endpoints
        // --------------------------


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
