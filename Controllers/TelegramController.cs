using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IPOClient.Models.Requests.Telegram;
using IPOClient.Services.Interfaces;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    [Authorize]
    public class TelegramController : ControllerBase
    {
        private readonly ITelegramConfigService _telegramConfigService;

        public TelegramController(ITelegramConfigService telegramConfigService)
        {
            _telegramConfigService = telegramConfigService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var result = await _telegramConfigService.GetByUserIdAsync(userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateOrUpdate(int userId, [FromBody] TelegramConfigRequest request)
        {
            var result = await _telegramConfigService.CreateOrUpdateAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
    }
}
