
using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Services.Implementations;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/orderremark")]
    [Authorize]
    public class OrderRemarkController : ControllerBase
    {
        private readonly IIPOOrderRemarkService _remarkService;

        public OrderRemarkController(IIPOOrderRemarkService remarkService)
        {
            _remarkService = remarkService;
        }
        /// <summary>
        /// Create a new order remark
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrderRemark([FromBody] CreateOrderRemarkRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _remarkService.CreateOrderRemarkAsync(request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }
        /// <summary>
        /// Get a remark DTO
        /// </summary>
        [HttpGet("remarkdto/{ipoId}")]
        public async Task<IActionResult> GetRemarkDTO(int ipoId)
        {
            var companyId = GetCompanyId();
            var result = await _remarkService.GetOrderRemarkDTOByCompanyAsync(companyId, ipoId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }
        private int GetUserId()
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
