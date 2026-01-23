using IPOClient.Models.Requests.ClientSetup;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IPOClient.Controllers
{
    [Route("api/client-setup")]
    [ApiController]
    [Authorize]
    public class ClientSetupController : ControllerBase
    {
        private readonly IClientSetupService _clientSetupService;

        public ClientSetupController(IClientSetupService clientSetupService)
        {
            _clientSetupService = clientSetupService;
        }

        /// <summary>
        /// Create a new client
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientSetupRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _clientSetupService.CreateClientAsync(request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Update an existing client
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientSetupRequest request)
        {
            if (id != request.ClientId)
                return BadRequest(new { message = "ID mismatch" });

            var userId = GetUserId();
            var result = await _clientSetupService.UpdateClientAsync(request, userId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Delete a client (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _clientSetupService.DeleteClientAsync(id, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get a client by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientById(int id, [FromQuery] bool includeDeleted = false)
        {
            var companyId = GetCompanyId();
            var result = await _clientSetupService.GetClientByIdAsync(id, companyId, includeDeleted);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get clients with pagination and filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetClients([FromQuery] ClientSetupFilterRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _clientSetupService.GetClientsAsync(request, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Delete all clients and create history record
        /// </summary>
        [HttpPost("delete-all")]
        public async Task<IActionResult> DeleteAllClients([FromBody] DeleteAllClientsRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var result = await _clientSetupService.DeleteAllClientsAsync(request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get delete history with pagination
        /// </summary>
        [HttpGet("delete-history")]
        public async Task<IActionResult> GetDeleteHistory([FromQuery] ClientDeleteHistoryFilterRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _clientSetupService.GetDeleteHistoryAsync(request, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get delete history by ID with details
        /// </summary>
        [HttpGet("delete-history/{historyId}")]
        public async Task<IActionResult> GetDeleteHistoryById(int historyId)
        {
            var companyId = GetCompanyId();
            var result = await _clientSetupService.GetDeleteHistoryByIdAsync(historyId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private int GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }
    }
}
