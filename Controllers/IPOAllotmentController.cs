using IPOClient.Data;
using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/allotment")]
    [Authorize]
    public class IPOAllotmentController : ControllerBase
    {
        private readonly IIPOAllotmentService _allotmentService;
        private readonly IPOClientDbContext _dbContext;

        public IPOAllotmentController(IIPOAllotmentService allotmentService, IPOClientDbContext dbContext)
        {
            _allotmentService = allotmentService;
            _dbContext = dbContext;
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
            if (request.IpoId <= 0)
                return BadRequest(ReturnData<BulkAllotmentCheckResponse>.ErrorResponse("IpoId is required.", 400));

            var companyId = GetCompanyId();
            var result = await _allotmentService.FirmAllotmentAsync(request, companyId);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get all IPOs from all registrars (scraped in parallel, unified list tagged with registrar)
        /// </summary>
        [HttpGet("ipos-all")]
        public async Task<IActionResult> GetAllIPOs()
        {
            var result = await _allotmentService.GetAllIPOsAsync();
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        /// <summary>
        /// Get current/upcoming IPOs from NSE India
        /// </summary>
        [HttpGet("ipos-nse")]
        public async Task<IActionResult> GetIPOsFromNSE()
        {
            var result = await _allotmentService.GetIPOsFromNSEAsync();
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

        /// <summary>
        /// Get cache status for all registrars (admin diagnostic endpoint)
        /// </summary>
        [HttpGet("cache-status")]
        public async Task<IActionResult> GetCacheStatus()
        {
            var cacheEntries = await _dbContext.IPO_RegistrarCache
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.RegistrarName,
                    c.CachedIpoCount,
                    c.LastFetchedAt,
                    c.LastFailedAt,
                    c.LastErrorMessage,
                    AgeMinutes = (int)EF.Functions.DateDiffMinute(c.LastFetchedAt, DateTime.UtcNow)
                })
                .OrderBy(c => c.RegistrarName)
                .ToListAsync();

            return Ok(ReturnData<object>.SuccessResponse(cacheEntries,
                $"Cache status for {cacheEntries.Count} registrars"));
        }

        /// <summary>
        /// Force refresh cache for a specific registrar (triggers live scrape + cache update)
        /// </summary>
        [HttpPost("cache-refresh/{registrar}")]
        public async Task<IActionResult> RefreshCache(string registrar)
        {
            var result = await _allotmentService.GetIPOsByRegistrarAsync(registrar);
            return StatusCode(result.ResponseCode ?? 500, result);
        }

        private int GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("cid")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }
    }
}
