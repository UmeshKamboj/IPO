using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IPOClient.Models.Requests.Profile;
using IPOClient.Services.Interfaces;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _profileService.GetProfileAsync(GetCurrentUserId());

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var result = await _profileService.ChangePasswordAsync(GetCurrentUserId(), request);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
