using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IPOClient.Models.Requests.Email;
using IPOClient.Services.Interfaces;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/email")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        private readonly IEmailConfigService _emailConfigService;

        public EmailController(IEmailConfigService emailConfigService)
        {
            _emailConfigService = emailConfigService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var result = await _emailConfigService.GetByUserIdAsync(userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateOrUpdate(int userId, [FromBody] EmailConfigRequest request)
        {
            var result = await _emailConfigService.CreateOrUpdateAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
    }
}
