using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Services.Interfaces;
using System.Security.Claims;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all users with global search and pagination (Admin only)
        /// </summary>
        /// <remarks>
        /// Returns paginated list of all users with optional global search across Email, FirstName, LastName, Phone.
        /// Only accessible to users with IsAdmin = true.
        /// Default: skip=0, pageSize=10
        /// </remarks>
        /// <param name="request">Search and paging parameters (searchValue, skip, pageSize)</param>
        /// <returns>Paginated list of users</returns>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">Unauthorized - token invalid or expired</response>
        /// <response code="403">Forbidden - user is not admin</response>
        [HttpPost("list")]
        public async Task<IActionResult> GetAllUsers([FromBody] UserFilterRequest request)
        {
            if (!IsAdmin())
                return Forbid();

            var result = await _userService.GetAllUsersAsync(request);
            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Get user by ID (Admin only or own profile)
        /// </summary>
        /// <remarks>
        /// Admin can view any user. Regular users can only view their own profile.
        /// </remarks>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        /// <response code="200">User retrieved successfully</response>
        /// <response code="401">Unauthorized - token invalid or expired</response>
        /// <response code="403">Forbidden - cannot access this user's profile</response>
        /// <response code="404">User not found</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id, GetCurrentUserId(), IsAdmin());
            
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Create new user (Admin only)
        /// </summary>
        /// <remarks>
        /// Only users with IsAdmin = true can create new users.
        /// Email must be unique and password must be provided.
        /// </remarks>
        /// <param name="request">User creation details</param>
        /// <returns>Created user with ID</returns>
        /// <response code="201">User created successfully</response>
        /// <response code="400">Bad request - invalid input or email already exists</response>
        /// <response code="401">Unauthorized - token invalid or expired</response>
        /// <response code="403">Forbidden - user is not admin</response>
        /// <response code="500">Server error</response>
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!IsAdmin())
                return Forbid();

            var result = await _userService.CreateUserAsync(request, GetCurrentUserId());
            
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return CreatedAtAction(nameof(GetUserById), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Update user (Admin for all fields, Users for own phone only)
        /// </summary>
        /// <remarks>
        /// Admin can update all user fields.
        /// Regular users can only update their own phone number.
        /// </remarks>
        /// <param name="id">User ID to update</param>
        /// <param name="request">Updated user details</param>
        /// <returns>Updated user</returns>
        /// <response code="200">User updated successfully</response>
        /// <response code="400">Bad request - invalid input</response>
        /// <response code="401">Unauthorized - token invalid or expired</response>
        /// <response code="403">Forbidden - cannot update this user</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Server error</response>
        [HttpPut("{id}/update")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var result = await _userService.UpdateUserAsync(id, request, GetCurrentUserId(), IsAdmin());
            
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        /// <remarks>
        /// Only users with IsAdmin = true can delete users.
        /// Cannot delete the currently logged-in user.
        /// </remarks>
        /// <param name="id">User ID to delete</param>
        /// <returns>Success message</returns>
        /// <response code="200">User deleted successfully</response>
        /// <response code="401">Unauthorized - token invalid or expired</response>
        /// <response code="403">Forbidden - user is not admin or cannot delete this user</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin())
                return Forbid();

            var result = await _userService.DeleteUserAsync(id);
            
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        private bool IsAdmin()
        {
            var isAdminClaim = User.FindFirst("role")?.Value;
            return isAdminClaim == "Admin";
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
