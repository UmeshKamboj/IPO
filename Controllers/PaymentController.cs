using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Services.Implementations;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [Route("api/payment")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentTransactionService _paymentService;

        public PaymentController(IPaymentTransactionService paymentService)
        {
            _paymentService = paymentService;
        }
        /// <summary>
        /// Create a payment transaction
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _paymentService.CreatePaymentAsync(request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }
        /// <summary>
        /// Create a ipo to ipo payment transaction
        /// </summary>
        [HttpPost("ipototipo/create")]
        public async Task<IActionResult> CreateIPOtoIPOPayment([FromBody] CreateIPOToIPOPaymentRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _paymentService.CreateIPOtoIPOPaymentAsync(request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }
        /// <summary>
        /// Get Payment Transaction by Id
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentTransactionById(int id)
        {
            var companyId = GetCompanyId();
            var result = await _paymentService.GetPaymentTransactionByIdAsync(id, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }
        //helpers to get user and company id from claims
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
