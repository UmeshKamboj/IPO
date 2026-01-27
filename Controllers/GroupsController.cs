using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPOClient.Controllers
{
    [Route("api/groups")]
    [ApiController]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        { 
            _groupService = groupService;
        } 
         

        /// <summary>
        /// Get simple list of all groups for dropdown (ipoGroupId, groupName only)
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetGroups()
        {
            var companyId = GetCompanyId();
            var result = await _groupService.GetGroupListAsync(companyId);
            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 500, result);
            return Ok(result);
        } 
        // --------------------------
        // Advanced Group Operations (with pagination and filters)
        // --------------------------

        /// <summary>
        /// Create a new group with advanced properties
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateAdvancedGroup([FromBody] CreateGroupRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _groupService.CreateGroupAsync(request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Update an existing group with advanced properties
        /// </summary>
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateAdvancedGroup(int id, [FromBody] UpdateGroupRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();
            var result = await _groupService.UpdateGroupAsync(id, request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Delete a group (soft delete) - advanced version
        /// </summary>
        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> DeleteAdvancedGroup(int id)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _groupService.DeleteGroupAsync(id, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get a group by ID with advanced properties
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAdvancedGroupById(int id)
        {
            var companyId = GetCompanyId();
            var result = await _groupService.GetGroupByIdAsync(id, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get groups with pagination, global search, and filters
        /// </summary>
        [HttpPost("all")]
        public async Task<IActionResult> GetAdvancedGroups([FromBody] GroupFilterRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _groupService.GetGroupsAsync(request, companyId);
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
