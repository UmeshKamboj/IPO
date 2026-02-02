using IPOClient.Models.Requests.GroupWiseDashboard;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Services.Implementations;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [Route("api/GroupWiseDashboard")]
    [ApiController]
    [Authorize]
    public class GroupWiseDashboardController : ControllerBase
    {
        private readonly IGroupwiseDashboardService _groupwisedashboardService;

        public GroupWiseDashboardController(IGroupwiseDashboardService groupwisedashboardService)
        {
            _groupwisedashboardService = groupwisedashboardService;
        }

        /// <summary>
        /// Get group wise Summary Dashboard List
        /// </summary>
        /// <returns></returns>
        [HttpPost("list")]
        public async Task<IActionResult> GetGroupWiseSummaryList([FromBody] GroupWiseSummaryRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _groupwisedashboardService.GetGroupWiseDashboardPagedListAsync(request, companyId);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
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
