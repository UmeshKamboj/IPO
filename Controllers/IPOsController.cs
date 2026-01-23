using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/ipos")]
    [Authorize]
    public class IPOsController : ControllerBase
    {
        private readonly IIPOService _ipoService;

        public IPOsController(IIPOService ipoService)
        {
            _ipoService = ipoService;
        }

        /// <summary>
        /// Get all IPOs with global search and pagination
        /// </summary>
        /// <remarks>
        /// Returns paginated list of IPOs with optional global search across IPOName, Remark.
        /// Default: skip=0, pageSize=10
        /// </remarks>
        /// <param name="request">Search and paging parameters (searchValue, skip, pageSize)</param>
        /// <returns>Paginated list of IPOs</returns>
        [HttpPost("list")]
        public async Task<IActionResult> GetAllIPOs([FromBody] IPOFilterRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _ipoService.GetAllIPOsAsync(request, companyId);

            var statusCode = result.ResponseCode == 200 ? 200 : 400;
            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Get IPO by ID
        /// </summary>
        /// <param name="id">IPO ID</param>
        /// <returns>IPO details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetIPOById(int id)
        {
            var companyId = GetCompanyId();
            var result = await _ipoService.GetIPOByIdAsync(id, companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Create new IPO
        /// </summary>
        /// <param name="request">IPO creation details</param>
        /// <returns>Created IPO</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateIPO([FromBody] CreateIPORequest request)
        {
            var userId = GetCurrentUserId();
            var companyId = GetCompanyId();

            var result = await _ipoService.CreateIPOAsync(request, userId, companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return CreatedAtAction(nameof(GetIPOById), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Update IPO
        /// </summary>
        /// <param name="id">IPO ID to update</param>
        /// <param name="request">Updated IPO details</param>
        [HttpPut("{id}/update")]
        public async Task<IActionResult> UpdateIPO(int id, [FromBody] CreateIPORequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _ipoService.UpdateIPOAsync(id, request, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }

        /// <summary>
        /// Delete IPO (soft delete)
        /// </summary>
        /// <param name="id">IPO ID to delete</param>
        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> DeleteIPO(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _ipoService.DeleteIPOAsync(id, userId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }
        /// <summary>
        /// Get IPO name + id list for current company
        /// </summary>
        [HttpGet("getiponames")]
        public async Task<IActionResult> GetIPONamesByCompanyId()
        {
            var companyId = GetCompanyId();
            var result = await _ipoService.GetIPONameIdByCompanyAsync(companyId);

            if (!result.Success)
                return StatusCode(result.ResponseCode ?? 400, result);

            return Ok(result);
        }




        // Helpers to get claims
        private int GetCurrentUserId()
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
