using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Responses;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    [Authorize]
    public class IPOAnalysisController : ControllerBase
    {
        private readonly IIPOAnalysisService _analysisService;

        public IPOAnalysisController(IIPOAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }

        /// <summary>
        /// Calculate analysis without saving (preview)
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] IPOAnalysisRequest request)
        {
            var companyId = GetCompanyId();
            var result = await _analysisService.CalculateAnalysisAsync(request, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Calculate and save analysis to database
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] IPOAnalysisRequest request)
        {
            var userId = GetCurrentUserId();
            var companyId = GetCompanyId();
            var result = await _analysisService.SubmitAnalysisAsync(request, userId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get available analysis types
        /// </summary>
        [HttpGet("types")]
        public IActionResult GetAnalysisTypes()
        {
            var types = new[]
            {
                new { Id = 1, Name = "Initial P&L Analysis" },
                new { Id = 2, Name = "After Allotment P&L" },
                new { Id = 3, Name = "After Listing" }
            };
            return Ok(ReturnData<object>.SuccessResponse(types));
        }

        /// <summary>
        /// Get all saved analyses for an IPO
        /// </summary>
        [HttpGet("{ipoId}")]
        public async Task<IActionResult> GetAllAnalyses(int ipoId)
        {
            var companyId = GetCompanyId();
            var result = await _analysisService.GetAllAnalysesAsync(ipoId, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get saved analysis for a specific tab type
        /// </summary>
        [HttpGet("{ipoId}/{analysisType}")]
        public async Task<IActionResult> GetAnalysisByType(int ipoId, int analysisType)
        {
            var companyId = GetCompanyId();
            var result = await _analysisService.GetAnalysisAsync(ipoId, analysisType, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

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
