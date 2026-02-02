using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/allotment")]
    [Authorize]
    public class IPOAllotmentController : ControllerBase
    {
        private readonly IIPOAllotmentService _allotmentService;

        public IPOAllotmentController(IIPOAllotmentService allotmentService)
        {
            _allotmentService = allotmentService;
        }

        /// <summary>
        /// Get list of IPOs for a registrar (used to populate IPO Name dropdown)
        /// </summary>
        /// <param name="registrar">Registrar name: Linkin, Kfintech, BigShare, Purva, SkyLine, Integrated, Maashitla, Cambridge</param>
        [HttpGet("ipos/{registrar}")]
        public async Task<IActionResult> GetIPOsByRegistrar(string registrar)
        {
            var result = await _allotmentService.GetIPOsByRegistrarAsync(registrar);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Bulk allotment check - Submit button in IPO Allotment Check dialog
        /// Fetches all PANs for the IPO, checks allotment on registrar site, updates DB
        /// </summary>
        [HttpPost("check")]
        public async Task<IActionResult> BulkAllotmentCheck([FromBody] BulkAllotmentCheckRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Registrar) ||
                string.IsNullOrWhiteSpace(request.CompanyCode) ||
                request.IpoId <= 0)
            {
                return BadRequest(ReturnData<BulkAllotmentCheckResponse>.ErrorResponse("Registrar, CompanyCode, and IpoId are required.", 400));
            }

            var companyId = GetCompanyId();
            var result = await _allotmentService.BulkAllotmentCheckAsync(request, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Firm allotment - sets AllotedQty for order children filtered by Group and InvestorType
        /// InvestorType: 0/null = All, 1 = Retail (Kostak+SubjectTo), 2 = SHNI (Kostak+SubjectTo), 3 = BHNI (Kostak+SubjectTo)
        /// </summary>
        [HttpPost("firm-allotment")]
        public async Task<IActionResult> FirmAllotment([FromBody] FirmAllotmentRequest request)
        {
            if (request.IpoId <= 0 || request.GroupId <= 0)
                return BadRequest(ReturnData<BulkAllotmentCheckResponse>.ErrorResponse("IpoId and GroupId are required.", 400));

            var companyId = GetCompanyId();
            var result = await _allotmentService.FirmAllotmentAsync(request, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get list of supported registrars
        /// </summary>
        [HttpGet("registrars")]
        public IActionResult GetRegistrars()
        {
            var registrars = new[]
            {
                new { Value = "Linkin", Label = "MUFG Intime (Link Intime)" },
                new { Value = "Kfintech", Label = "KFin Technologies" },
                new { Value = "BigShare", Label = "Bigshare Services" },
                new { Value = "Purva", Label = "Purva Sharegistry" },
                new { Value = "SkyLine", Label = "Skyline Financial Services" },
                new { Value = "Integrated", Label = "Integrated Registry" },
                new { Value = "Maashitla", Label = "Maashitla Securities" },
                new { Value = "Cambridge", Label = "Cameo Corporate Services" }
            };

            return Ok(ReturnData<object>.SuccessResponse(registrars));
        }

        private int GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("cid")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }
    }
}
