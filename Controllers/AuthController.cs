using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Services.Interfaces;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Login with email and password to get JWT token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Logout and invalidate current session
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return Ok(ReturnData.SuccessResponse("Logged out successfully", 200));
        }

        // TODO: Re-enable refresh token endpoint later
        // [HttpPost("refresh-token")]
        // [Authorize]
        // public async Task<IActionResult> RefreshToken()
        // {
        //     var result = await _authService.RefreshTokenAsync();
        //
        //     if (!result.Success)
        //         return StatusCode(result.ResponseCode ?? 400, result);
        //
        //     return Ok(result);
        // }
    }
}

