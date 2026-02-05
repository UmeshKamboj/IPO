using IPOClient.Models.Enums;
using IPOClient.Services.Implementations;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IPOClient.Controllers
{
    [ApiController]
    [Route("api/backup")]
    [Authorize]
    public class IPOBackupController : ControllerBase
    {
        private readonly IIPOBackupService _ipoBackupService;
        public IPOBackupController(IIPOBackupService ipoBackupService)
        {
            _ipoBackupService = ipoBackupService;
        }
        /// <summary>
        /// Download IPO Backup File
        /// </summary>
        /// <returns>File</returns>
        [HttpGet("ipobackup")]
        public async Task<IActionResult> IPOBackupFile(int ipoId)
        {
            var companyId = GetCompanyId();
            var userName = GetUserName();
            var result = await _ipoBackupService.IPOBackupAsync(ipoId, companyId, userName);
            if (!result.Success || result.Data == null)
                return BadRequest(result);
            return File(result.Data.Bytes, result.Data.ContentType, result.Data.FileName);
        }
        /// <summary>
        /// Download All IPO Backup File
        /// </summary>
        /// <returns>File</returns>
        [HttpGet("allipobackup")]
        public async Task<IActionResult> AllIPOBackupFile()
        {
            var companyId = GetCompanyId();
            var userName = GetUserName();
            var result = await _ipoBackupService.AllIPOsBackupAsync(companyId, userName);
            if (!result.Success || result.Data == null)
                return BadRequest(result);
            return File(result.Data.Bytes, result.Data.ContentType, result.Data.FileName);
        }
        /// <summary>
        /// Download  Accounting IPO Backup File
        /// </summary>
        /// <returns>File</returns>
        [HttpGet("accounting")]
        public async Task<IActionResult> AllIPOAccountingBackupFile()
        {
            var userName = GetUserName();
            var companyId = GetCompanyId();
            var result = await _ipoBackupService.IPOAccountingBackupAsync(companyId, userName);
            if (!result.Success || result.Data == null)
                return BadRequest(result);
            return File(result.Data.Bytes, result.Data.ContentType, result.Data.FileName);
        }
        // Helpers to get claims
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
        private string GetUserName()
        {
            var userNameClaim = User.FindFirst("name")?.Value;
            return userNameClaim ?? string.Empty;
        }
        private int GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("cid")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }
    }
}
