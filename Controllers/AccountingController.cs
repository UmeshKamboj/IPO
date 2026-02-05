using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/accounting")]
    [Authorize]
    public class AccountingController : ControllerBase
    {
        private readonly IIPOAccountingService _ipoAccountingService;
        public AccountingController(IIPOAccountingService ipoAccountingService)
        {
            _ipoAccountingService = ipoAccountingService;
        }
        /// <summary>
        /// Get groups with pagination, global search, and filters
        /// </summary>
        //[HttpPost("list")]
        //public async Task<IActionResult> GetPagedAccountingList([FromBody] PaymentListRequest request)
        //{
        //    var companyId = GetCompanyId();
        //    var result = await _ipoAccountingService.GetPagedAccountingListAsync(request, companyId);
        //    return StatusCode(result.ResponseCode ?? 500, result);
        //}

        /// <summary>
        /// Get accounting data for deleted IPO and deleted Group with IsDeleted checkbox and DateRange filter
        /// </summary>
        [HttpPost("deleted-list")]
        public async Task<IActionResult> GetDeletedAccountingList([FromBody] DeletedAccountingListRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _ipoAccountingService.GetDeletedAccountingListAsync(request, companyId);
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
