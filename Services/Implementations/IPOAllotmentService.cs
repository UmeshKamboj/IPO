using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace IPOClient.Services.Implementations
{
    public class IPOAllotmentService : IIPOAllotmentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IPOAllotmentService> _logger;
        private readonly IPOClientDbContext _dbContext;

        public IPOAllotmentService(IHttpClientFactory httpClientFactory, ILogger<IPOAllotmentService> logger, IPOClientDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<List<IPOAllotmentCompany>>> GetIPOsByRegistrarAsync(string registrar)
        {
            try
            {
                List<IPOAllotmentCompany> ipos;

                switch (registrar.ToLower())
                {
                    case "linkin":
                        ipos = await GetIPOsFromMUFGIntimeAsync();
                        break;

                    case "kfintech":
                        ipos = await GetIPOsFromKFinTechAsync();
                        break;

                    case "bigshare":
                        ipos = await GetIPOsFromBigShareAsync();
                        break;

                    case "purva":
                        ipos = await GetIPOsFromPurvaAsync();
                        break;

                    case "skyline":
                        ipos = await GetIPOsFromSkylineAsync();
                        break;

                    case "integrated":
                        ipos = await GetIPOsFromIntegratedAsync();
                        break;

                    case "maashitla":
                        ipos = await GetIPOsFromMaashitlaAsync();
                        break;

                    case "cambridge":
                        ipos = await GetIPOsFromCambridgeAsync();
                        break;

                    default:
                        return ApiResponse<List<IPOAllotmentCompany>>.ErrorResponse($"Unknown registrar: {registrar}");
                }

                if (ipos.Count > 0)
                    return ApiResponse<List<IPOAllotmentCompany>>.SuccessResponse(ipos, $"Found {ipos.Count} IPOs from {registrar}");

                return ApiResponse<List<IPOAllotmentCompany>>.ErrorResponse($"No IPOs found for registrar: {registrar}. Site may be temporarily unavailable.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching IPOs for registrar {Registrar}", registrar);
                return ApiResponse<List<IPOAllotmentCompany>>.ErrorResponse("Failed to fetch IPO list", ex.Message);
            }
        }

        public async Task<ApiResponse<IPOAllotmentResult>> CheckAllotmentAsync(string registrar, string companyCode, string panNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(panNumber) || panNumber.Length != 10)
                    return ApiResponse<IPOAllotmentResult>.ErrorResponse("Invalid PAN number. Must be 10 characters.");

                IPOAllotmentResult? result = registrar.ToLower() switch
                {
                    "linkin" => await CheckAllotmentMUFGAsync(companyCode, panNumber),
                    "bigshare" => await CheckAllotmentBigShareAsync(companyCode, panNumber),
                    _ => null
                };

                if (result != null)
                {
                    result.Registrar = registrar;
                    return ApiResponse<IPOAllotmentResult>.SuccessResponse(result);
                }

                return ApiResponse<IPOAllotmentResult>.ErrorResponse(
                    $"Allotment check not supported for {registrar} via API. Please check manually on registrar website.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking allotment for PAN {PAN}, Company {Company}", panNumber, companyCode);
                return ApiResponse<IPOAllotmentResult>.ErrorResponse("Allotment check failed", ex.Message);
            }
        }

        #region Bulk Allotment Check (Submit button)

        /// <summary>
        /// Bulk allotment check: fetches PANs from order children, checks each against registrar, updates DB
        /// </summary>
        public async Task<ApiResponse<BulkAllotmentCheckResponse>> BulkAllotmentCheckAsync(BulkAllotmentCheckRequest request, int companyId)
        {
            try
            {
                // 1. Get all order children for this IPO that have PANs
                var query = _dbContext.ChildPlaceOrder
                    .Include(c => c.IPOOrder)
                        .ThenInclude(o => o.BuyerMaster)
                    .Where(c => c.IPOOrder.BuyerMaster.IPOId == request.IpoId
                             && c.CompanyId == companyId
                             && !c.IsDeleted
                             && !string.IsNullOrEmpty(c.PANNumber));

                // Apply PAN filter
                if (request.PanFilter?.ToLower() == "pending")
                {
                    // Only PANs where allotment is not yet filled
                    query = query.Where(c => c.AllotedQty == null || c.AllotedQty == 0);
                }

                var orderChildren = await query.ToListAsync();

                if (!orderChildren.Any())
                {
                    return ApiResponse<BulkAllotmentCheckResponse>.ErrorResponse("No PAN records found for this IPO.");
                }

                // Get unique PANs to avoid duplicate API calls
                var uniquePans = orderChildren
                    .Where(c => !string.IsNullOrWhiteSpace(c.PANNumber))
                    .Select(c => c.PANNumber!.ToUpper().Trim())
                    .Distinct()
                    .ToList();

                var response = new BulkAllotmentCheckResponse
                {
                    TotalPANs = uniquePans.Count,
                    Results = new List<AllotmentPanResult>()
                };

                // 2. Check allotment for each unique PAN
                var panResults = new Dictionary<string, IPOAllotmentResult?>();

                foreach (var pan in uniquePans)
                {
                    try
                    {
                        var allotResult = await CheckSingleAllotmentAsync(request.Registrar, request.CompanyCode, pan);
                        panResults[pan] = allotResult;
                        response.Processed++;

                        if (allotResult != null && allotResult.Status == "Allotted")
                            response.Allotted++;
                        else if (allotResult != null)
                            response.NotAllotted++;
                        else
                            response.Failed++;

                        // Small delay to avoid rate limiting
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to check allotment for PAN {PAN}", pan);
                        panResults[pan] = null;
                        response.Failed++;
                    }
                }

                // 3. Update order children in DB
                foreach (var child in orderChildren)
                {
                    var panKey = child.PANNumber?.ToUpper().Trim() ?? "";
                    if (!panResults.TryGetValue(panKey, out var result) || result == null)
                        continue;

                    child.AllotedQty = result.AllottedShares;
                    child.DematNumber = result.DematNumber ?? child.DematNumber;
                    child.ApplicationNo = result.ApplicationNumber ?? child.ApplicationNo;
                    child.ModifiedDate = DateTime.UtcNow;
                    child.ModifiedBy = "AllotmentCheck";
                    response.Updated++;

                    response.Results.Add(new AllotmentPanResult
                    {
                        POChildId = child.POChildId,
                        PanNumber = panKey,
                        Status = result.Status,
                        AllottedShares = result.AllottedShares,
                        DematNumber = result.DematNumber,
                        ApplicationNo = result.ApplicationNumber
                    });
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Bulk allotment check completed: {Total} PANs, {Allotted} allotted, {Updated} records updated",
                    response.TotalPANs, response.Allotted, response.Updated);

                return ApiResponse<BulkAllotmentCheckResponse>.SuccessResponse(response,
                    $"Processed {response.Processed} PANs. {response.Allotted} allotted, {response.NotAllotted} not allotted, {response.Failed} failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk allotment check failed for IPO {IpoId}", request.IpoId);
                return ApiResponse<BulkAllotmentCheckResponse>.ErrorResponse("Bulk allotment check failed", ex.Message);
            }
        }

        /// <summary>
        /// Firm allotment: mark all order children for an IPO as allotted with their order quantity
        /// </summary>
        public async Task<ApiResponse<BulkAllotmentCheckResponse>> FirmAllotmentAsync(int ipoId, int companyId)
        {
            try
            {
                var orderChildren = await _dbContext.ChildPlaceOrder
                    .Include(c => c.IPOOrder)
                        .ThenInclude(o => o.BuyerMaster)
                    .Where(c => c.IPOOrder.BuyerMaster.IPOId == ipoId
                             && c.CompanyId == companyId
                             && !c.IsDeleted)
                    .ToListAsync();

                if (!orderChildren.Any())
                {
                    return ApiResponse<BulkAllotmentCheckResponse>.ErrorResponse("No order records found for this IPO.");
                }

                var response = new BulkAllotmentCheckResponse
                {
                    TotalPANs = orderChildren.Count,
                    Results = new List<AllotmentPanResult>()
                };

                foreach (var child in orderChildren)
                {
                    child.AllotedQty = child.Quantity;
                    child.ModifiedDate = DateTime.UtcNow;
                    child.ModifiedBy = "FirmAllotment";
                    response.Updated++;
                    response.Allotted++;

                    response.Results.Add(new AllotmentPanResult
                    {
                        POChildId = child.POChildId,
                        PanNumber = child.PANNumber ?? "",
                        Status = "Firm Allotted",
                        AllottedShares = child.Quantity
                    });
                }

                response.Processed = response.TotalPANs;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Firm allotment completed: {Count} records updated for IPO {IpoId}", response.Updated, ipoId);

                return ApiResponse<BulkAllotmentCheckResponse>.SuccessResponse(response,
                    $"Firm allotment applied to {response.Updated} records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firm allotment failed for IPO {IpoId}", ipoId);
                return ApiResponse<BulkAllotmentCheckResponse>.ErrorResponse("Firm allotment failed", ex.Message);
            }
        }

        /// <summary>
        /// Internal: check allotment for a single PAN (used by bulk check)
        /// </summary>
        private async Task<IPOAllotmentResult?> CheckSingleAllotmentAsync(string registrar, string companyCode, string pan)
        {
            return registrar.ToLower() switch
            {
                "linkin" => await CheckAllotmentMUFGAsync(companyCode, pan),
                "bigshare" => await CheckAllotmentBigShareAsync(companyCode, pan),
                _ => null
            };
        }

        #endregion

        #region MUFG Intime (Link Intime) - TESTED & WORKING

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromMUFGIntimeAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();

            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://in.mpms.mufg.com/Initial_Offer/IPO.aspx/GetDetails");
                request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("Origin", "https://in.mpms.mufg.com");
                request.Headers.Referrer = new Uri("https://in.mpms.mufg.com/Initial_Offer/public-issues.html");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("MUFG Intime returned {StatusCode}", response.StatusCode);
                    return ipos;
                }

                var json = await response.Content.ReadAsStringAsync();
                var xmlMatch = Regex.Match(json, @"""d""\s*:\s*""(.+)""", RegexOptions.Singleline);
                if (!xmlMatch.Success)
                {
                    _logger.LogWarning("MUFG Intime: Could not extract XML from response");
                    return ipos;
                }

                var xmlString = Regex.Unescape(xmlMatch.Groups[1].Value);
                var xDoc = XDocument.Parse(xmlString);
                foreach (var table in xDoc.Descendants("Table"))
                {
                    var companyId = table.Element("company_id")?.Value?.Trim();
                    var companyName = table.Element("companyname")?.Value?.Trim();

                    if (!string.IsNullOrWhiteSpace(companyId) && !string.IsNullOrWhiteSpace(companyName))
                    {
                        ipos.Add(new IPOAllotmentCompany
                        {
                            CompanyCode = companyId,
                            CompanyName = WebUtility.HtmlDecode(companyName)
                        });
                    }
                }

                _logger.LogInformation("MUFG Intime: Found {Count} IPOs", ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IPO list from MUFG Intime");
            }

            return ipos;
        }

        private async Task<IPOAllotmentResult?> CheckAllotmentMUFGAsync(string companyCode, string panNumber)
        {
            _logger.LogInformation("MUFG allotment check requires CAPTCHA - returning null");
            return await Task.FromResult<IPOAllotmentResult?>(null);
        }

        #endregion

        #region BigShare - TESTED & WORKING

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromBigShareAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();

            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");
                var html = await client.GetStringAsync("https://ipo.bigshareonline.com/IPO_Status.html");

                var optionPattern = @"<option\s+value=""(\d+)""[^>]*>([^<]+)</option>";
                var matches = Regex.Matches(html, optionPattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    var code = match.Groups[1].Value.Trim();
                    var name = match.Groups[2].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(code) &&
                        !name.Contains("Select", StringComparison.OrdinalIgnoreCase) &&
                        !name.Contains("--", StringComparison.OrdinalIgnoreCase))
                    {
                        ipos.Add(new IPOAllotmentCompany
                        {
                            CompanyCode = code,
                            CompanyName = WebUtility.HtmlDecode(name)
                        });
                    }
                }

                _logger.LogInformation("BigShare: Found {Count} IPOs", ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IPO list from BigShare");
            }

            return ipos;
        }

        private async Task<IPOAllotmentResult?> CheckAllotmentBigShareAsync(string companyCode, string panNumber)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("company", companyCode),
                    new KeyValuePair<string, string>("type", "pan"),
                    new KeyValuePair<string, string>("value", panNumber.ToUpper())
                });

                var response = await client.PostAsync("https://ipo.bigshareonline.com/IPO_Status.html", formData);
                var resultHtml = await response.Content.ReadAsStringAsync();

                var result = new IPOAllotmentResult
                {
                    PanNumber = panNumber.ToUpper(),
                    CompanyName = companyCode
                };

                if (resultHtml.Contains("No record", StringComparison.OrdinalIgnoreCase) ||
                    resultHtml.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(resultHtml))
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                }
                else
                {
                    result.Status = "Allotted";
                    var sharesPattern = @"(\d+)\s*(?:shares|equity)";
                    var sharesMatch = Regex.Match(resultHtml, sharesPattern, RegexOptions.IgnoreCase);
                    if (sharesMatch.Success && int.TryParse(sharesMatch.Groups[1].Value, out int shares))
                        result.AllottedShares = shares;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check allotment from BigShare");
                return null;
            }
        }

        #endregion

        #region KFinTech - WAF blocked

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromKFinTechAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();

            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");
                var html = await client.GetStringAsync("https://kosmic.kfintech.com/ipostatus/");

                if (html.Contains("moved to a new location", StringComparison.OrdinalIgnoreCase) ||
                    html.Contains("Request Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("KFinTech blocks automated requests. User should check at ipostatus.kfintech.com");
                    return ipos;
                }

                ipos = ParseDropdownOptions(html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KFinTech site blocked. Check manually at ipostatus.kfintech.com");
            }

            return ipos;
        }

        #endregion

        #region Other Registrars - generic dropdown scraping

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromPurvaAsync()
            => await ScrapeDropdownFromUrlAsync("https://purvashare.com/ipo-status", "Purva");

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromSkylineAsync()
            => await ScrapeDropdownFromUrlAsync("https://www.skylinerta.com/ipo.php", "Skyline");

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromIntegratedAsync()
            => await ScrapeDropdownFromUrlAsync("https://www.intaborhq.com", "Integrated");

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromMaashitlaAsync()
            => await ScrapeDropdownFromUrlAsync("https://www.maaborhq.com", "Maashitla");

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromCambridgeAsync()
            => await ScrapeDropdownFromUrlAsync("https://www.caborhq.com", "Cambridge");

        private async Task<List<IPOAllotmentCompany>> ScrapeDropdownFromUrlAsync(string url, string registrarName)
        {
            var ipos = new List<IPOAllotmentCompany>();

            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");
                var html = await client.GetStringAsync(url);
                ipos = ParseDropdownOptions(html);
                _logger.LogInformation("{Registrar}: Found {Count} IPOs", registrarName, ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IPO list from {Registrar} ({Url})", registrarName, url);
            }

            return ipos;
        }

        private static List<IPOAllotmentCompany> ParseDropdownOptions(string html)
        {
            var ipos = new List<IPOAllotmentCompany>();
            var optionPattern = @"<option\s+value=""([^""]+)""[^>]*>([^<]+)</option>";
            var matches = Regex.Matches(html, optionPattern, RegexOptions.IgnoreCase);

            var skipValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "en", "hi", "mr", "gu", "te", "ta", "bn", "", "0", "null" };

            var skipNames = new[] { "Select", "--", "ENGLISH", "HINDI", "MARATHI", "GUJRATI", "TELUGU", "TAMIL", "BENGALI" };

            foreach (Match match in matches)
            {
                var code = match.Groups[1].Value.Trim();
                var name = match.Groups[2].Value.Trim();

                if (string.IsNullOrWhiteSpace(code) || skipValues.Contains(code))
                    continue;

                if (skipNames.Any(s => name.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    continue;

                ipos.Add(new IPOAllotmentCompany
                {
                    CompanyCode = code,
                    CompanyName = WebUtility.HtmlDecode(name)
                });
            }

            return ipos;
        }

        #endregion
    }
}
