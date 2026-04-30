using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
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

        public async Task<ReturnData<List<IPOAllotmentCompany>>> GetIPOsByRegistrarAsync(string registrar)
        {
            try
            {
                List<IPOAllotmentCompany>? ipos = null;
                string? scrapeError = null;

                // Step 1: Validate registrar name
                var validRegistrars = new[] { "linkin", "kfintech", "bigshare", "purva", "skyline", "integrated", "maashitla", "cambridge" };
                if (!validRegistrars.Contains(registrar.ToLower()))
                    return ReturnData<List<IPOAllotmentCompany>>.ErrorResponse($"Unknown registrar: {registrar}");

                // Step 2: Attempt live scrape
                try
                {
                    ipos = registrar.ToLower() switch
                    {
                        "linkin" => await GetIPOsFromMUFGIntimeAsync(),
                        "kfintech" => await GetIPOsFromKFinTechAsync(),
                        "bigshare" => await GetIPOsFromBigShareAsync(),
                        "purva" => await GetIPOsFromPurvaAsync(),
                        "skyline" => await GetIPOsFromSkylineAsync(),
                        "integrated" => await GetIPOsFromIntegratedAsync(),
                        "maashitla" => await GetIPOsFromMaashitlaAsync(),
                        "cambridge" => await GetIPOsFromCambridgeAsync(),
                        _ => new List<IPOAllotmentCompany>()
                    };
                }
                catch (Exception ex)
                {
                    scrapeError = ex.Message;
                    _logger.LogWarning(ex, "Live scrape failed for {Registrar}, will try cache", registrar);
                    ipos = new List<IPOAllotmentCompany>();
                }

                // Step 3: Live scrape succeeded with data → update cache and return
                if (ipos.Count > 0)
                {
                    _ = UpdateCacheAsync(registrar, ipos); // Fire and forget — don't slow down response
                    return ReturnData<List<IPOAllotmentCompany>>.SuccessResponse(ipos, $"Found {ipos.Count} IPOs from {registrar}");
                }

                // Step 4: Live scrape returned empty or failed → fall back to cache
                var (cachedIpos, lastFetched) = await LoadFromCacheAsync(registrar, scrapeError ?? "Live scrape returned 0 results");

                if (cachedIpos != null && cachedIpos.Count > 0 && lastFetched.HasValue)
                {
                    var ageText = FormatCacheAge(DateTime.UtcNow - lastFetched.Value);
                    return ReturnData<List<IPOAllotmentCompany>>.WarningResponse(
                        $"{registrar} site is temporarily unavailable. Showing {cachedIpos.Count} cached IPOs (last updated {ageText} ago).",
                        206, cachedIpos);
                }

                // Step 5: No cache available either
                return ReturnData<List<IPOAllotmentCompany>>.ErrorResponse(
                    $"No IPOs found for registrar: {registrar}. Site may be temporarily unavailable and no cached data exists.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching IPOs for registrar {Registrar}", registrar);

                // Last resort: try cache even on unexpected errors
                var (cachedIpos, lastFetched) = await LoadFromCacheAsync(registrar, ex.Message);
                if (cachedIpos != null && cachedIpos.Count > 0 && lastFetched.HasValue)
                {
                    var ageText = FormatCacheAge(DateTime.UtcNow - lastFetched.Value);
                    return ReturnData<List<IPOAllotmentCompany>>.WarningResponse(
                        $"Error occurred but serving cached data for {registrar} (cached {ageText} ago).",
                        206, cachedIpos);
                }

                return ReturnData<List<IPOAllotmentCompany>>.ErrorResponse($"Failed to fetch IPO list: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<IPOAllotmentResult>> CheckAllotmentAsync(string registrar, string companyCode, string panNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(panNumber) || panNumber.Length != 10)
                    return ReturnData<IPOAllotmentResult>.ErrorResponse("Invalid PAN number. Must be 10 characters.");

                IPOAllotmentResult? result = registrar.ToLower() switch
                {
                    "linkin" => await CheckAllotmentMUFGAsync(companyCode, panNumber),
                    "kfintech" => await CheckAllotmentKFinTechAsync(companyCode, panNumber),
                    "bigshare" => await CheckAllotmentBigShareAsync(companyCode, panNumber),
                    "skyline" => await CheckAllotmentSkylineAsync(companyCode, panNumber),
                    "integrated" => await CheckAllotmentIntegratedAsync(companyCode, panNumber),
                    "maashitla" => await CheckAllotmentMaashitlaAsync(companyCode, panNumber),
                    "cambridge" => await CheckAllotmentCambridgeAsync(companyCode, panNumber),
                    _ => null
                };

                if (result != null)
                {
                    result.Registrar = registrar;
                    return ReturnData<IPOAllotmentResult>.SuccessResponse(result);
                }

                return ReturnData<IPOAllotmentResult>.ErrorResponse(
                    $"Allotment check not supported for {registrar} via API. Please check manually on registrar website.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking allotment for PAN {PAN}, Company {Company}", panNumber, companyCode);
                return ReturnData<IPOAllotmentResult>.ErrorResponse($"Allotment check failed: {ex.Message}", 500);
            }
        }

        #region Bulk Allotment Check (Submit button)

        /// <summary>
        /// Bulk allotment check: fetches PANs from order children, checks each against registrar, updates DB
        /// </summary>
        public async Task<ReturnData<BulkAllotmentCheckResponse>> BulkAllotmentCheckAsync(BulkAllotmentCheckRequest request, int companyId)
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
                    return ReturnData<BulkAllotmentCheckResponse>.ErrorResponse("No PAN records found for this IPO.");
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
                        {
                            response.Failed++;
                            response.Errors.Add($"PAN {pan}: Registrar returned no data");
                        }

                        // Small delay to avoid rate limiting
                        await Task.Delay(200);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "Failed to check allotment for PAN {PAN}", pan);
                        panResults[pan] = null;
                        response.Failed++;
                        response.Errors.Add($"PAN {pan}: Registrar server error - {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to check allotment for PAN {PAN}", pan);
                        panResults[pan] = null;
                        response.Failed++;
                        response.Errors.Add($"PAN {pan}: {ex.Message}");
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

                // If all PANs failed, return error response so frontend knows it's a failure
                if (response.Failed > 0 && response.Allotted == 0 && response.NotAllotted == 0)
                {
                    return ReturnData<BulkAllotmentCheckResponse>.ErrorResponse(
                        $"Allotment check failed for all {response.Failed} PANs. The {request.Registrar} registrar server returned an error. Please try again later or check the registrar website directly.",
                        500, response);
                }

                // If some failed but some succeeded, return success with warning
                if (response.Failed > 0)
                {
                    return ReturnData<BulkAllotmentCheckResponse>.SuccessResponse(response,
                        $"Processed {response.Processed} PANs. {response.Allotted} allotted, {response.NotAllotted} not allotted, {response.Failed} failed (registrar server error).");
                }

                return ReturnData<BulkAllotmentCheckResponse>.SuccessResponse(response,
                    $"Processed {response.Processed} PANs. {response.Allotted} allotted, {response.NotAllotted} not allotted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk allotment check failed for IPO {IpoId}", request.IpoId);
                return ReturnData<BulkAllotmentCheckResponse>.ErrorResponse($"Bulk allotment check failed: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Firm allotment: sets AllotedQty for order children filtered by Group and InvestorType
        /// </summary>
        public async Task<ReturnData<BulkAllotmentCheckResponse>> FirmAllotmentAsync(FirmAllotmentRequest request, int companyId)
        {
            try
            {
                // Step 1: Get matching OrderIds using indexed columns on BuyerMaster + BuyerOrder
                // This avoids a 3-table JOIN in the UPDATE and lets SQL use existing indexes efficiently
                var kostakCategories = new[] { (int)IPOOrderCategory.Kostak, (int)IPOOrderCategory.SubjectTo };

                var orderQuery = _dbContext.BuyerOrders
                    .Where(o => o.BuyerMaster.IPOId == request.IpoId
                             && o.BuyerMaster.CompanyId == companyId
                             && o.BuyerMaster.IsActive
                             && !o.BuyerMaster.IsDeleted
                             && !o.IsDeleted
                             && kostakCategories.Contains(o.OrderCategory));

                if (request.InvestorType.HasValue && request.InvestorType.Value > 0)
                    orderQuery = orderQuery.Where(o => o.InvestorType == request.InvestorType.Value);

                var matchingOrderIds = await orderQuery.Select(o => o.OrderId).ToListAsync();

                if (!matchingOrderIds.Any())
                    return ReturnData<BulkAllotmentCheckResponse>.ErrorResponse("No order records found for the selected filters.");

                // Step 2: Bulk UPDATE children — filter on direct columns using existing index
                var childQuery = _dbContext.ChildPlaceOrder
                    .Where(c => matchingOrderIds.Contains(c.OrderId)
                             && c.CompanyId == companyId
                             && !c.IsDeleted);

                if (request.GroupId > 0)
                    childQuery = childQuery.Where(c => c.GroupId == request.GroupId);

                var updatedCount = await childQuery.ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.AllotedQty, request.AllotedQty)
                    .SetProperty(c => c.ModifiedDate, DateTime.UtcNow)
                    .SetProperty(c => c.ModifiedBy, "FirmAllotment"));

                var response = new BulkAllotmentCheckResponse
                {
                    TotalPANs = updatedCount,
                    Processed = updatedCount,
                    Updated = updatedCount,
                    Allotted = updatedCount,
                    Results = new List<AllotmentPanResult>()
                };

                _logger.LogInformation("Firm allotment completed: {Count} records updated for IPO {IpoId}, Group {GroupId}, InvestorType {InvestorType}",
                    updatedCount, request.IpoId, request.GroupId, request.InvestorType);

                return ReturnData<BulkAllotmentCheckResponse>.SuccessResponse(response,
                    $"Firm allotment applied to {updatedCount} records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firm allotment failed for IPO {IpoId}", request.IpoId);
                return ReturnData<BulkAllotmentCheckResponse>.ErrorResponse($"Firm allotment failed: {ex.Message}", 500);
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
                "kfintech" => await CheckAllotmentKFinTechAsync(companyCode, pan),
                "bigshare" => await CheckAllotmentBigShareAsync(companyCode, pan),
                "skyline" => await CheckAllotmentSkylineAsync(companyCode, pan),
                "integrated" => await CheckAllotmentIntegratedAsync(companyCode, pan),
                "maashitla" => await CheckAllotmentMaashitlaAsync(companyCode, pan),
                "cambridge" => await CheckAllotmentCambridgeAsync(companyCode, pan),
                _ => null
            };
        }

        #endregion

        #region MUFG Intime (Link Intime) - Updated for new portal at in.mpms.mufg.com

        private const string MufgBaseUrl = "https://in.mpms.mufg.com/Initial_Offer/";
        private const string MufgOrigin = "https://in.mpms.mufg.com";
        private const string MufgReferer = "https://in.mpms.mufg.com/Initial_Offer/public-issues.html";

        private static string MufgAesEncrypt(string plainText)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes("8080808080808080");
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = keyBytes;
            aes.IV = keyBytes;
            aes.Mode = System.Security.Cryptography.CipherMode.CBC;
            aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            using var encryptor = aes.CreateEncryptor();
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(encrypted);
        }

        private async Task<string> MufgGenerateTokenAsync(HttpClient client)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, MufgBaseUrl + "IPO.aspx/generateToken");
                req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                req.Headers.Add("X-Requested-With", "XMLHttpRequest");
                req.Headers.Add("Origin", MufgOrigin);
                req.Headers.Referrer = new Uri(MufgReferer);

                var resp = await client.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();
                var match = Regex.Match(json, @"""d""\s*:\s*""([^""]+)""");
                if (match.Success)
                    return MufgAesEncrypt(match.Groups[1].Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MUFG generateToken failed");
            }
            return "";
        }

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromMUFGIntimeAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();

            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                var request = new HttpRequestMessage(HttpMethod.Post, MufgBaseUrl + "IPO.aspx/GetDetails");
                request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("Origin", MufgOrigin);
                request.Headers.Referrer = new Uri(MufgReferer);

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
                            CompanyName = WebUtility.HtmlDecode(companyName),
                            Registrar = "Linkin"
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
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                // Step 1: Get encrypted session token (required by new portal)
                var token = await MufgGenerateTokenAsync(client);

                // Step 2: Search by PAN — CHKVAL=1 means PAN search mode on the new portal
                var request = new HttpRequestMessage(HttpMethod.Post, MufgBaseUrl + "IPO.aspx/SearchOnPan");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("Origin", MufgOrigin);
                request.Headers.Referrer = new Uri(MufgReferer);

                var jsonBody = $"{{\"clientid\":\"{companyCode}\",\"PAN\":\"{panNumber.ToUpper()}\",\"IFSC\":\"\",\"CHKVAL\":\"1\",\"token\":\"{token}\"}}";
                request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    var errorSnippet = responseText?.Length > 300 ? responseText[..300] : responseText;
                    _logger.LogWarning("MUFG allotment check returned {StatusCode} for PAN {PAN}. Response: {Response}",
                        response.StatusCode, panNumber, errorSnippet);
                    throw new Exception($"MUFG returned HTTP {(int)response.StatusCode}: {errorSnippet}");
                }

                var result = new IPOAllotmentResult
                {
                    PanNumber = panNumber.ToUpper(),
                    CompanyName = companyCode,
                    Registrar = "Linkin"
                };

                // Decode JSON-escaped XML from the "d" field
                var xmlMatch = Regex.Match(responseText, @"""d""\s*:\s*""(.+?)""(?:\s*\})", RegexOptions.Singleline);
                if (!xmlMatch.Success)
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                    return result;
                }

                // JSON unicode escapes (\u003c etc.) — use JsonSerializer to decode properly
                var xmlString = System.Text.Json.JsonSerializer.Deserialize<string>($"\"{xmlMatch.Groups[1].Value}\"") ?? "";
                var xDoc = XDocument.Parse(xmlString.Trim());

                // Table1/Msg = error or "No Record Found" message from server
                var msgEl = xDoc.Descendants("Table1").FirstOrDefault()?.Element("Msg");
                if (msgEl != null)
                {
                    _logger.LogDebug("MUFG SearchOnPan returned Msg for PAN {PAN}: {Msg}", panNumber, msgEl.Value);
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                    return result;
                }

                // Table = allotment record; empty <NewDataSet /> means no record
                var table = xDoc.Descendants("Table").FirstOrDefault();
                if (table == null)
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                    return result;
                }

                // Fields: ALLOT = shares allotted, PEMNDG = IPO name
                var allotText = table.Element("ALLOT")?.Value?.Trim();
                if (int.TryParse(allotText, out int allotted) && allotted > 0)
                {
                    result.Status = "Allotted";
                    result.AllottedShares = allotted;
                }
                else
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                }
                result.CompanyName = table.Element("PEMNDG")?.Value?.Trim() ?? companyCode;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MUFG allotment check failed for PAN {PAN}", panNumber);
                throw; // Let the bulk check catch this and add to Errors list
            }
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
                            CompanyName = WebUtility.HtmlDecode(name),
                            Registrar = "BigShare"
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

        #region KFinTech - HTTP-only (extracts IPO list from JS bundle via Wayback Machine)

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromKFinTechAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();
            var client = _httpClientFactory.CreateClient("Registrar");

            // KFinTech embeds the IPO list as a hardcoded JSON array inside their React JS bundle.
            // CloudFront WAF blocks direct HTTP access (TLS fingerprinting).
            // We use Wayback Machine to access cached copies of the site.

            // Strategy 1: Use CDX API to directly find the most recent JS bundle snapshot
            try
            {
                _logger.LogInformation("KFinTech: Trying Wayback CDX API to find JS bundle...");

                // CDX API returns all archived copies of the JS bundle, sorted by most recent first
                // Note: Use wildcard (*) in URL without matchType=prefix — CDX treats * as glob wildcard only when matchType is not set
                // Also filter by statuscode:200 to skip failed captures (status '-' = ~870 byte HTML wrapper, not actual JS)
                var cdxUrl = "https://web.archive.org/cdx/search/cdx?url=ipostatus.kfintech.com/static/js/main*&output=json&limit=10&fl=timestamp,original&filter=statuscode:200&sort=desc";
                using var cdxReq = new HttpRequestMessage(HttpMethod.Get, cdxUrl);
                cdxReq.Headers.Add("User-Agent", "Mozilla/5.0");

                var cdxResp = await client.SendAsync(cdxReq);
                if (cdxResp.IsSuccessStatusCode)
                {
                    var cdxJson = await cdxResp.Content.ReadAsStringAsync();
                    using var cdxDoc = JsonDocument.Parse(cdxJson);
                    var cdxArray = cdxDoc.RootElement;

                    // First row is header ["timestamp","original"], rest are data
                    if (cdxArray.GetArrayLength() > 1)
                    {
                        for (int i = 1; i < cdxArray.GetArrayLength(); i++)
                        {
                            var row = cdxArray[i];
                            var timestamp = row[0].GetString() ?? "";
                            var originalUrl = row[1].GetString() ?? "";

                            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(originalUrl)) continue;

                            // Fetch the JS bundle using id_ to get raw content without Wayback rewrites
                            var jsUrl = $"https://web.archive.org/web/{timestamp}id_/{originalUrl}";
                            _logger.LogInformation("KFinTech: Fetching JS bundle from {Url}", jsUrl);

                            using var jsReq = new HttpRequestMessage(HttpMethod.Get, jsUrl);
                            jsReq.Headers.Add("User-Agent", "Mozilla/5.0");

                            var jsResp = await client.SendAsync(jsReq);
                            if (jsResp.IsSuccessStatusCode)
                            {
                                var jsContent = await jsResp.Content.ReadAsStringAsync();

                                // Extract embedded JSON: JSON.parse('[{"clientId":"...","name":"..."},...]')
                                var jsonMatch = Regex.Match(jsContent, @"JSON\.parse\('(\[.*?\])'\)");
                                if (jsonMatch.Success)
                                {
                                    ipos = ParseKFinTechApiResponse(jsonMatch.Groups[1].Value);
                                    if (ipos.Count > 0)
                                    {
                                        _logger.LogInformation("KFinTech (CDX): Found {Count} IPOs from snapshot {Timestamp}", ipos.Count, timestamp);
                                        return ipos;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KFinTech: CDX API approach failed");
            }

            // Strategy 2: Trigger Wayback Machine to save a fresh snapshot, then fetch it
            // This ensures we get the latest data even if CDX hasn't crawled recently
            try
            {
                _logger.LogInformation("KFinTech: Triggering Wayback Machine save for fresh snapshot...");

                // Step 1: Request Wayback Machine to save/crawl the page now
                using var saveReq = new HttpRequestMessage(HttpMethod.Get,
                    "https://web.archive.org/save/https://ipostatus.kfintech.com");
                saveReq.Headers.Add("User-Agent", "Mozilla/5.0");

                // Fire and don't wait too long — just trigger the crawl
                var saveCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    await client.SendAsync(saveReq, saveCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Save request may take a while, that's OK — we just triggered it
                    _logger.LogDebug("KFinTech: Wayback save request timed out (expected, crawl continues in background)");
                }

                // Step 2: Now use the standard Wayback API to get the latest snapshot
                using var wbApiReq = new HttpRequestMessage(HttpMethod.Get,
                    "https://archive.org/wayback/available?url=ipostatus.kfintech.com");
                wbApiReq.Headers.Add("User-Agent", "Mozilla/5.0");

                var wbApiResp = await client.SendAsync(wbApiReq);
                if (wbApiResp.IsSuccessStatusCode)
                {
                    var wbJson = await wbApiResp.Content.ReadAsStringAsync();
                    using var wbDoc = JsonDocument.Parse(wbJson);

                    var snapshotUrl = "";
                    if (wbDoc.RootElement.TryGetProperty("archived_snapshots", out var snapshots) &&
                        snapshots.TryGetProperty("closest", out var closest) &&
                        closest.TryGetProperty("url", out var urlProp))
                    {
                        snapshotUrl = urlProp.GetString() ?? "";
                    }

                    if (!string.IsNullOrEmpty(snapshotUrl))
                    {
                        // Fetch the cached HTML page to find the JS bundle filename
                        using var htmlReq = new HttpRequestMessage(HttpMethod.Get, snapshotUrl);
                        htmlReq.Headers.Add("User-Agent", "Mozilla/5.0");

                        var htmlResp = await client.SendAsync(htmlReq);
                        if (htmlResp.IsSuccessStatusCode)
                        {
                            var html = await htmlResp.Content.ReadAsStringAsync();

                            // Extract JS filename like main.26d2acc4.js
                            var jsMatch = Regex.Match(html, @"main\.([a-f0-9]+)\.js");
                            if (jsMatch.Success)
                            {
                                var jsFileName = jsMatch.Value;
                                var tsMatch = Regex.Match(snapshotUrl, @"/web/(\d{14})/");
                                var timestamp = tsMatch.Success ? tsMatch.Groups[1].Value : "";

                                if (!string.IsNullOrEmpty(timestamp))
                                {
                                    // Fetch the JS bundle using id_ for raw content
                                    var jsUrl = $"https://web.archive.org/web/{timestamp}id_/https://ipostatus.kfintech.com/static/js/{jsFileName}";

                                    using var jsReq = new HttpRequestMessage(HttpMethod.Get, jsUrl);
                                    jsReq.Headers.Add("User-Agent", "Mozilla/5.0");

                                    var jsResp = await client.SendAsync(jsReq);
                                    if (jsResp.IsSuccessStatusCode)
                                    {
                                        var jsContent = await jsResp.Content.ReadAsStringAsync();

                                        var jsonMatch = Regex.Match(jsContent, @"JSON\.parse\('(\[.*?\])'\)");
                                        if (jsonMatch.Success)
                                        {
                                            ipos = ParseKFinTechApiResponse(jsonMatch.Groups[1].Value);
                                            if (ipos.Count > 0)
                                            {
                                                _logger.LogInformation("KFinTech (Wayback): Found {Count} IPOs from snapshot {Timestamp}",
                                                    ipos.Count, timestamp);
                                                return ipos;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KFinTech: Wayback Machine save+fetch approach failed");
            }

            _logger.LogWarning("KFinTech: All HTTP strategies failed. 0 IPOs found.");
            return ipos;
        }

        private List<IPOAllotmentCompany> ParseKFinTechApiResponse(string json)
        {
            var ipos = new List<IPOAllotmentCompany>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // The API returns an array of objects with clientId and name
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        var clientId = item.TryGetProperty("clientId", out var cidProp) ? cidProp.GetString() :
                                       item.TryGetProperty("client_id", out var cid2Prop) ? cid2Prop.GetString() : "";
                        var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() :
                                   item.TryGetProperty("companyName", out var cnProp) ? cnProp.GetString() : "";

                        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(name))
                        {
                            ipos.Add(new IPOAllotmentCompany
                            {
                                CompanyCode = clientId,
                                CompanyName = name,
                                Registrar = "KFinTech"
                            });
                        }
                    }
                }
                // Could also be an object with a data array
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    JsonElement dataElement = root;
                    if (root.TryGetProperty("data", out var dataProp))
                        dataElement = dataProp;
                    else if (root.TryGetProperty("result", out var resProp))
                        dataElement = resProp;

                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataElement.EnumerateArray())
                        {
                            var clientId = item.TryGetProperty("clientId", out var cidProp) ? cidProp.GetString() :
                                           item.TryGetProperty("client_id", out var cid2Prop) ? cid2Prop.GetString() : "";
                            var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() :
                                       item.TryGetProperty("companyName", out var cnProp) ? cnProp.GetString() : "";

                            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(name))
                            {
                                ipos.Add(new IPOAllotmentCompany
                                {
                                    CompanyCode = clientId,
                                    CompanyName = name,
                                    Registrar = "KFinTech"
                                });
                            }
                        }
                    }
                }

                _logger.LogInformation("KFinTech API response parsed: {Count} IPOs", ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse KFinTech API response");
            }

            return ipos;
        }

        private async Task<IPOAllotmentResult?> CheckAllotmentKFinTechAsync(string companyCode, string panNumber)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                // KFinTech uses AWS API Gateway: GET with client_id (IPO code) and reqparam (PAN) headers
                var apiUrl = "https://0uz601ms56.execute-api.ap-south-1.amazonaws.com/prod/api/query?type=pan";

                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Add("client_id", companyCode);
                request.Headers.Add("reqparam", panNumber.ToUpper());
                request.Headers.Add("Origin", "https://ipostatus.kfintech.com");
                request.Headers.Add("Referer", "https://ipostatus.kfintech.com/");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "application/json, text/plain, */*");

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                var result = new IPOAllotmentResult
                {
                    PanNumber = panNumber.ToUpper(),
                    CompanyName = companyCode,
                    Registrar = "KFinTech"
                };

                if (string.IsNullOrWhiteSpace(json) || json.Contains("\"error\"", StringComparison.OrdinalIgnoreCase) ||
                    json.Contains("No record", StringComparison.OrdinalIgnoreCase) ||
                    json.Contains("Unexpected error", StringComparison.OrdinalIgnoreCase))
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                    return result;
                }

                // Parse JSON response for allotment details
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Response could be object or array
                JsonElement data = root;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataProp))
                    data = dataProp;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() == 0)
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                    return result;
                }

                // Extract allotment info
                var item = data.ValueKind == JsonValueKind.Array ? data[0] : data;

                var allottedShares = 0;
                if (item.TryGetProperty("allottedShares", out var sharesProp))
                    int.TryParse(sharesProp.ToString(), out allottedShares);
                else if (item.TryGetProperty("shares", out var shares2Prop))
                    int.TryParse(shares2Prop.ToString(), out allottedShares);
                else if (item.TryGetProperty("quantity", out var qtyProp))
                    int.TryParse(qtyProp.ToString(), out allottedShares);

                result.AllottedShares = allottedShares;
                result.Status = allottedShares > 0 ? "Allotted" : "Not Allotted";

                if (item.TryGetProperty("applicationNumber", out var appNoProp))
                    result.ApplicationNumber = appNoProp.GetString() ?? "";
                else if (item.TryGetProperty("appNo", out var appNo2))
                    result.ApplicationNumber = appNo2.GetString() ?? "";

                if (item.TryGetProperty("dpId", out var dpProp))
                    result.DematNumber = dpProp.GetString();
                else if (item.TryGetProperty("dematNumber", out var dematProp))
                    result.DematNumber = dematProp.GetString();

                if (item.TryGetProperty("name", out var nameProp))
                    result.CompanyName = nameProp.GetString() ?? companyCode;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KFinTech allotment check failed for PAN {PAN}", panNumber);
                return null;
            }
        }

        #endregion

        #region Other Registrars - generic dropdown scraping

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromPurvaAsync()
            => await ScrapeDropdownFromUrlAsync("https://purvashare.com/investor-service/ipo-query", "Purva", "Purva");

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromSkylineAsync()
            => await ScrapeDropdownFromUrlAsync("https://www.skylinerta.com/ipo.php", "Skyline", "SkyLine");

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromIntegratedAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                // Company list is loaded via AJAX POST (dropdown is dynamic, not server-rendered)
                var content = new StringContent("Req=1&Comp=IPO", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await client.PostAsync("https://www.integratedregistry.in/IRMS_V2/RegistrarsToAjax.aspx", content);
                var html = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(html) || html.Contains("NO RECORD", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Integrated: No records returned");
                    return ipos;
                }

                // Response is raw HTML options: <option value='AEL'>Avana Electrosystems Limited</option>
                var optionPattern = @"<option\s+value=['""]?([^'"">\s]+)['""]?[^>]*>([^<]+)</option>";
                var matches = Regex.Matches(html, optionPattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    var code = match.Groups[1].Value.Trim();
                    var name = match.Groups[2].Value.Trim();

                    if (string.IsNullOrWhiteSpace(code) || code == "0" ||
                        name.Contains("select", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("--"))
                        continue;

                    ipos.Add(new IPOAllotmentCompany
                    {
                        CompanyCode = code,
                        CompanyName = WebUtility.HtmlDecode(name),
                        Registrar = "Integrated"
                    });
                }

                _logger.LogInformation("Integrated: Found {Count} IPOs", ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IPO list from Integrated Registry");
            }

            return ipos;
        }

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromMaashitlaAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();

            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");
                var json = await client.GetStringAsync("https://microservices.maashitla.com/public-issues-service/companies");

                var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in dataArr.EnumerateArray())
                    {
                        var id = item.TryGetProperty("companyId", out var cidProp) ? cidProp.ToString() :
                                 item.TryGetProperty("id", out var idProp) ? idProp.ToString() :
                                 item.TryGetProperty("_id", out var _idProp) ? _idProp.ToString() : "";
                        var title = item.TryGetProperty("companyTitle", out var ctProp) ? ctProp.GetString() :
                                    item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() :
                                    item.TryGetProperty("companyName", out var nameProp) ? nameProp.GetString() : "";

                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title))
                        {
                            ipos.Add(new IPOAllotmentCompany
                            {
                                CompanyCode = id,
                                CompanyName = title,
                                Registrar = "Maashitla"
                            });
                        }
                    }
                }

                _logger.LogInformation("Maashitla: Found {Count} IPOs", ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IPO list from Maashitla");
            }

            return ipos;
        }

        private async Task<List<IPOAllotmentCompany>> GetIPOsFromCambridgeAsync()
        {
            var ipos = new List<IPOAllotmentCompany>();
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");
                var html = await client.GetStringAsync("https://ipostatus1.cameoindia.com/");

                // Target only the company dropdown (drpCompany), not ddlUserTypes
                var selectPattern = @"<select[^>]*id=""drpCompany""[^>]*>(.*?)</select>";
                var selectMatch = Regex.Match(html, selectPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!selectMatch.Success)
                {
                    // Fallback: find the first select that contains "Limited" in options
                    var allSelects = Regex.Matches(html, @"<select[^>]*>(.*?)</select>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    foreach (Match s in allSelects)
                    {
                        if (s.Value.Contains("Limited", StringComparison.OrdinalIgnoreCase))
                        {
                            selectMatch = s;
                            break;
                        }
                    }
                }

                if (selectMatch.Success)
                {
                    var optionPattern = @"<option\s+value=""([^""]+)""[^>]*>([^<]+)</option>";
                    var matches = Regex.Matches(selectMatch.Value, optionPattern, RegexOptions.IgnoreCase);

                    foreach (Match match in matches)
                    {
                        var code = match.Groups[1].Value.Trim();
                        var name = match.Groups[2].Value.Trim();

                        if (string.IsNullOrWhiteSpace(code) || code == "0" ||
                            name.Contains("select", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("--"))
                            continue;

                        ipos.Add(new IPOAllotmentCompany
                        {
                            CompanyCode = code,
                            CompanyName = WebUtility.HtmlDecode(name),
                            Registrar = "Cambridge"
                        });
                    }
                }

                _logger.LogInformation("Cambridge/Cameo: Found {Count} IPOs", ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IPO list from Cambridge/Cameo");
            }

            return ipos;
        }

        private async Task<List<IPOAllotmentCompany>> ScrapeDropdownFromUrlAsync(string url, string registrarName, string registrarKey)
        {
            var ipos = new List<IPOAllotmentCompany>();

            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");
                var html = await client.GetStringAsync(url);
                ipos = ParseDropdownOptions(html, registrarKey);
                _logger.LogInformation("{Registrar}: Found {Count} IPOs", registrarName, ipos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IPO list from {Registrar} ({Url})", registrarName, url);
            }

            return ipos;
        }

        private static List<IPOAllotmentCompany> ParseDropdownOptions(string html, string registrar = "")
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
                    CompanyName = WebUtility.HtmlDecode(name),
                    Registrar = registrar
                });
            }

            return ipos;
        }

        #endregion

        #region Allotment Check - Skyline, Integrated, Maashitla

        private async Task<IPOAllotmentResult?> CheckAllotmentSkylineAsync(string companyCode, string panNumber)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                // Step 1: POST company selection to get the allotment form with CSRF token
                var step1Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("company", companyCode)
                });

                using var step1Req = new HttpRequestMessage(HttpMethod.Post, "https://www.skylinerta.com/display_application.php");
                step1Req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                step1Req.Content = step1Content;

                var step1Resp = await client.SendAsync(step1Req);
                var step1Html = await step1Resp.Content.ReadAsStringAsync();

                // Extract CSRF token
                var csrfMatch = Regex.Match(step1Html, @"name=""csrf_token""[^>]*value=""([^""]+)""");
                if (!csrfMatch.Success)
                    csrfMatch = Regex.Match(step1Html, @"value=""([^""]+)""[^>]*name=""csrf_token""");

                if (!csrfMatch.Success)
                {
                    _logger.LogWarning("Skyline: Could not extract CSRF token");
                    return null;
                }

                var csrfToken = csrfMatch.Groups[1].Value;

                // Step 2: POST allotment search with CSRF token
                var step2Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("company", companyCode),
                    new KeyValuePair<string, string>("action", "search"),
                    new KeyValuePair<string, string>("pan", panNumber.ToUpper()),
                    new KeyValuePair<string, string>("client_id", ""),
                    new KeyValuePair<string, string>("application_no", ""),
                    new KeyValuePair<string, string>("csrf_token", csrfToken)
                });

                // Must use same client for cookie continuity
                using var step2Req = new HttpRequestMessage(HttpMethod.Post, "https://www.skylinerta.com/display_application.php");
                step2Req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                step2Req.Content = step2Content;

                var step2Resp = await client.SendAsync(step2Req);
                var resultHtml = await step2Resp.Content.ReadAsStringAsync();

                var result = new IPOAllotmentResult
                {
                    PanNumber = panNumber.ToUpper(),
                    CompanyName = companyCode,
                    Registrar = "SkyLine"
                };

                if (resultHtml.Contains("No record found", StringComparison.OrdinalIgnoreCase) ||
                    resultHtml.Contains("session has expired", StringComparison.OrdinalIgnoreCase) ||
                    resultHtml.Contains("could not find", StringComparison.OrdinalIgnoreCase))
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                }
                else if (resultHtml.Contains("allot", StringComparison.OrdinalIgnoreCase) ||
                         resultHtml.Contains("shares", StringComparison.OrdinalIgnoreCase))
                {
                    result.Status = "Allotted";
                    var sharesMatch = Regex.Match(resultHtml, @"(\d+)\s*(?:shares|equity|Shares)", RegexOptions.IgnoreCase);
                    if (sharesMatch.Success && int.TryParse(sharesMatch.Groups[1].Value, out int shares))
                        result.AllottedShares = shares;

                    var appNoMatch = Regex.Match(resultHtml, @"(?:application|appl)[\s_.-]*(?:no|number|#)?[\s:]*([A-Z0-9]+)", RegexOptions.IgnoreCase);
                    if (appNoMatch.Success)
                        result.ApplicationNumber = appNoMatch.Groups[1].Value;
                }
                else
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skyline allotment check failed for PAN {PAN}", panNumber);
                return null;
            }
        }

        private async Task<IPOAllotmentResult?> CheckAllotmentIntegratedAsync(string companyCode, string panNumber)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                // Integrated uses simple AJAX POST - no CSRF, no CAPTCHA
                var content = new StringContent(
                    $"Req=2&Comp=IPO&CompCode={companyCode}&PAN={panNumber.ToUpper()}",
                    System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await client.PostAsync("https://www.integratedregistry.in/IRMS_V2/RegistrarsToAjax.aspx", content);
                var resultHtml = await response.Content.ReadAsStringAsync();

                var result = new IPOAllotmentResult
                {
                    PanNumber = panNumber.ToUpper(),
                    CompanyName = companyCode,
                    Registrar = "Integrated"
                };

                if (string.IsNullOrWhiteSpace(resultHtml) ||
                    resultHtml.Contains("No Record", StringComparison.OrdinalIgnoreCase) ||
                    resultHtml.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                }
                else
                {
                    // Parse HTML table response for allotment details
                    result.Status = "Allotted";
                    var sharesMatch = Regex.Match(resultHtml, @"(\d+)\s*(?:shares|equity|Shares)", RegexOptions.IgnoreCase);
                    if (sharesMatch.Success && int.TryParse(sharesMatch.Groups[1].Value, out int shares))
                        result.AllottedShares = shares;

                    // Try to extract application number from table
                    var appNoMatch = Regex.Match(resultHtml, @"<td[^>]*>\s*([A-Z0-9]{6,})\s*</td>", RegexOptions.IgnoreCase);
                    if (appNoMatch.Success)
                        result.ApplicationNumber = appNoMatch.Groups[1].Value;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Integrated allotment check failed for PAN {PAN}", panNumber);
                return null;
            }
        }

        private async Task<IPOAllotmentResult?> CheckAllotmentMaashitlaAsync(string companyCode, string panNumber)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");

                // Maashitla uses clean REST JSON API - no auth, no CAPTCHA
                var url = $"https://microservices.maashitla.com/public-issues-service/search?company={Uri.EscapeDataString(companyCode)}&pan={panNumber.ToUpper()}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                var result = new IPOAllotmentResult
                {
                    PanNumber = panNumber.ToUpper(),
                    CompanyName = companyCode,
                    Registrar = "Maashitla"
                };

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

                if (!success ||
                    (root.TryGetProperty("code", out var codeProp) && codeProp.GetString() == "NO_ALLOTMENT_FOUND"))
                {
                    result.Status = "Not Allotted";
                    result.AllottedShares = 0;
                    return result;
                }

                // Parse allotment data
                JsonElement data = root;
                if (root.TryGetProperty("data", out var dataProp))
                    data = dataProp;

                var item = data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0 ? data[0] : data;

                if (item.ValueKind == JsonValueKind.Object)
                {
                    var allottedShares = 0;
                    if (item.TryGetProperty("allottedShares", out var sharesProp))
                        int.TryParse(sharesProp.ToString(), out allottedShares);
                    else if (item.TryGetProperty("shares", out var shares2))
                        int.TryParse(shares2.ToString(), out allottedShares);
                    else if (item.TryGetProperty("quantity", out var qty))
                        int.TryParse(qty.ToString(), out allottedShares);

                    result.AllottedShares = allottedShares;
                    result.Status = allottedShares > 0 ? "Allotted" : "Not Allotted";

                    if (item.TryGetProperty("applicationNumber", out var appNo))
                        result.ApplicationNumber = appNo.GetString() ?? "";
                    if (item.TryGetProperty("name", out var name))
                        result.CompanyName = name.GetString() ?? companyCode;
                    if (item.TryGetProperty("dpId", out var dpId))
                        result.DematNumber = dpId.GetString();
                }
                else
                {
                    result.Status = "Allotted";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Maashitla allotment check failed for PAN {PAN}", panNumber);
                return null;
            }
        }

        private async Task<IPOAllotmentResult?> CheckAllotmentCambridgeAsync(string companyCode, string panNumber)
        {
            // Cambridge/Cameo requires session cookies to persist across GET page → GET CAPTCHA → POST form.
            // So we create a dedicated HttpClient with CookieContainer for each allotment check.
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var cookieContainer = new CookieContainer();
                    using var handler = new HttpClientHandler
                    {
                        CookieContainer = cookieContainer,
                        UseCookies = true,
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    using var client = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromSeconds(30)
                    };
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

                    // Step 1: GET the page to get session cookies + ViewState + CAPTCHA
                    var pageResp = await client.GetAsync("https://ipostatus1.cameoindia.com/");
                    var pageHtml = await pageResp.Content.ReadAsStringAsync();

                    // Extract ASP.NET hidden fields
                    var viewState = Regex.Match(pageHtml, @"id=""__VIEWSTATE""\s+value=""([^""]*?)""").Groups[1].Value;
                    var viewStateGen = Regex.Match(pageHtml, @"id=""__VIEWSTATEGENERATOR""\s+value=""([^""]*?)""").Groups[1].Value;
                    var eventValidation = Regex.Match(pageHtml, @"id=""__EVENTVALIDATION""\s+value=""([^""]*?)""").Groups[1].Value;

                    if (string.IsNullOrEmpty(viewState) || string.IsNullOrEmpty(eventValidation))
                    {
                        _logger.LogWarning("Cambridge: Could not extract ASP.NET ViewState (attempt {Attempt})", attempt);
                        continue;
                    }

                    // Step 2: Download the CAPTCHA image and solve it
                    var captchaTimestamp = DateTimeOffset.UtcNow.Ticks.ToString();
                    var captchaResp = await client.GetAsync(
                        $"https://ipostatus1.cameoindia.com/GenerateCaptcha.aspx?{captchaTimestamp}");
                    var captchaBytes = await captchaResp.Content.ReadAsByteArrayAsync();

                    var captchaText = SolveCameoCaptcha(captchaBytes);
                    if (string.IsNullOrEmpty(captchaText) || captchaText.Length != 6)
                    {
                        _logger.LogWarning("Cambridge: Failed to solve CAPTCHA (attempt {Attempt}, result: '{Captcha}')", attempt, captchaText);
                        continue;
                    }

                    _logger.LogInformation("Cambridge: Solved CAPTCHA as {Captcha} (attempt {Attempt})", captchaText, attempt);

                    // Step 3: Submit the allotment check form
                    var formData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("__EVENTTARGET", ""),
                        new KeyValuePair<string, string>("__EVENTARGUMENT", ""),
                        new KeyValuePair<string, string>("__VIEWSTATE", viewState),
                        new KeyValuePair<string, string>("__VIEWSTATEGENERATOR", viewStateGen),
                        new KeyValuePair<string, string>("__EVENTVALIDATION", eventValidation),
                        new KeyValuePair<string, string>("drpCompany", companyCode),
                        new KeyValuePair<string, string>("ddlUserTypes", "PAN NO"),
                        new KeyValuePair<string, string>("txtfolio", panNumber.ToUpper()),
                        new KeyValuePair<string, string>("txt_phy_captcha", captchaText),
                        new KeyValuePair<string, string>("btngenerate", "Submit")
                    });

                    using var submitReq = new HttpRequestMessage(HttpMethod.Post, "https://ipostatus1.cameoindia.com/");
                    submitReq.Headers.Add("Referer", "https://ipostatus1.cameoindia.com/");
                    submitReq.Content = formData;

                    var submitResp = await client.SendAsync(submitReq);
                    var resultHtml = await submitResp.Content.ReadAsStringAsync();

                    // Check for CAPTCHA error → retry
                    if (resultHtml.Contains("Captcha entered is incorrect", StringComparison.OrdinalIgnoreCase) ||
                        resultHtml.Contains("Please enter the Captcha", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Cambridge: CAPTCHA was incorrect (attempt {Attempt}/{Max})", attempt, maxRetries);
                        await Task.Delay(500); // Brief delay before retry
                        continue;
                    }

                    var result = new IPOAllotmentResult
                    {
                        PanNumber = panNumber.ToUpper(),
                        CompanyName = companyCode,
                        Registrar = "Cambridge"
                    };

                    // Check for "no record" or allotment result
                    if (resultHtml.Contains("No record", StringComparison.OrdinalIgnoreCase) ||
                        resultHtml.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                        resultHtml.Contains("No data", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Status = "Not Allotted";
                        result.AllottedShares = 0;
                    }
                    else if (resultHtml.Contains("showpop1(", StringComparison.OrdinalIgnoreCase))
                    {
                        // showpop1 is used for result display
                        var msgMatch = Regex.Match(resultHtml, @"showpop1\('([^']+)'");
                        if (msgMatch.Success)
                        {
                            var msg = msgMatch.Groups[1].Value;
                            if (msg.Contains("Not Allotted", StringComparison.OrdinalIgnoreCase) ||
                                msg.Contains("No record", StringComparison.OrdinalIgnoreCase))
                            {
                                result.Status = "Not Allotted";
                                result.AllottedShares = 0;
                            }
                            else
                            {
                                result.Status = "Allotted";
                                var sharesMatch = Regex.Match(msg, @"(\d+)\s*(?:shares|equity)", RegexOptions.IgnoreCase);
                                if (sharesMatch.Success && int.TryParse(sharesMatch.Groups[1].Value, out int shares))
                                    result.AllottedShares = shares;
                            }
                        }
                    }
                    else
                    {
                        // Try to parse table/grid result
                        var gridMatch = Regex.Match(resultHtml,
                            @"<table[^>]*id=""[^""]*(?:grd|grid|result)[^""]*""[^>]*>(.*?)</table>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        if (!gridMatch.Success)
                        {
                            gridMatch = Regex.Match(resultHtml,
                                @"<table[^>]*class=""[^""]*(?:result|data|grid)[^""]*""[^>]*>(.*?)</table>",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        }

                        if (gridMatch.Success)
                        {
                            var tableHtml = gridMatch.Groups[1].Value;
                            result.Status = "Allotted";

                            var sharesMatch = Regex.Match(tableHtml, @"(\d+)\s*(?:shares|equity|Shares)", RegexOptions.IgnoreCase);
                            if (sharesMatch.Success && int.TryParse(sharesMatch.Groups[1].Value, out int shares))
                                result.AllottedShares = shares;

                            var appNoMatch = Regex.Match(tableHtml, @"<td[^>]*>\s*([A-Z0-9]{6,})\s*</td>", RegexOptions.IgnoreCase);
                            if (appNoMatch.Success)
                                result.ApplicationNumber = appNoMatch.Groups[1].Value;
                        }
                        else
                        {
                            result.Status = "Not Allotted";
                            result.AllottedShares = 0;
                        }
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cambridge allotment check failed for PAN {PAN} (attempt {Attempt})", panNumber, attempt);
                    if (attempt == maxRetries) return null;
                    await Task.Delay(500);
                }
            }

            _logger.LogWarning("Cambridge: All {Max} CAPTCHA attempts failed for PAN {PAN}", maxRetries, panNumber);
            return null;
        }

        /// <summary>
        /// Simple OCR for Cameo's numeric CAPTCHA (6 digits, clean background, no distortion).
        /// Parses JPEG bytes, thresholds pixels, and matches digit patterns.
        /// </summary>
        private string SolveCameoCaptcha(byte[] imageBytes)
        {
            try
            {
                // The CAPTCHA is a simple JPEG: 100x30px, 6 dark digits on white background.
                // We use System.Drawing.Common to decode and read pixels.
                // System.Drawing.Common works on Windows; on Linux it needs libgdiplus.
                return SolveCaptchaWithDrawing(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CAPTCHA solve failed - System.Drawing may not be available on this platform");
                return "";
            }
        }

        /// <summary>
        /// Solve CAPTCHA using System.Drawing (Windows only)
        /// </summary>
        private string SolveCaptchaWithDrawing(byte[] imageBytes)
        {
            try
            {
                using var ms = new System.IO.MemoryStream(imageBytes);
                using var bitmap = new System.Drawing.Bitmap(ms);

                int width = bitmap.Width;   // 100
                int height = bitmap.Height; // 30

                // Convert to binary: dark pixels (text) = 1, light pixels (background) = 0
                var binary = new bool[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        // The digits are dark red/maroon on white. Threshold on brightness.
                        int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                        binary[x, y] = brightness < 160; // dark pixel = text
                    }
                }

                // Find digit columns by looking for vertical slices with dark pixels
                var colHasPixel = new bool[width];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (binary[x, y]) { colHasPixel[x] = true; break; }
                    }
                }

                // Find digit boundaries (contiguous runs of columns with pixels)
                var digitBounds = new List<(int start, int end)>();
                int? runStart = null;
                for (int x = 0; x < width; x++)
                {
                    if (colHasPixel[x] && runStart == null)
                        runStart = x;
                    else if (!colHasPixel[x] && runStart != null)
                    {
                        digitBounds.Add((runStart.Value, x - 1));
                        runStart = null;
                    }
                }
                if (runStart != null)
                    digitBounds.Add((runStart.Value, width - 1));

                // If we don't get 6 digits, try equal spacing
                if (digitBounds.Count != 6)
                {
                    digitBounds.Clear();
                    int digitWidth = width / 6;
                    for (int i = 0; i < 6; i++)
                        digitBounds.Add((i * digitWidth, (i + 1) * digitWidth - 1));
                }

                // Extract each digit as a normalized feature vector and match
                var captchaText = new System.Text.StringBuilder();
                foreach (var (start, end) in digitBounds)
                {
                    captchaText.Append(RecognizeDigit(binary, start, end, height));
                }

                return captchaText.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "System.Drawing CAPTCHA solve failed");
                return "";
            }
        }

        /// <summary>
        /// Recognize a single digit from a binary image slice using zone-based features.
        /// Divides the digit region into a 3x5 grid and counts dark pixels in each zone.
        /// </summary>
        private char RecognizeDigit(bool[,] binary, int xStart, int xEnd, int height)
        {
            int digitWidth = xEnd - xStart + 1;
            if (digitWidth <= 0) return '0';

            // Divide into 3 columns and 5 rows = 15 zones
            int zoneW = Math.Max(1, digitWidth / 3);
            int zoneH = Math.Max(1, height / 5);

            var zones = new double[3, 5];
            for (int zy = 0; zy < 5; zy++)
            {
                for (int zx = 0; zx < 3; zx++)
                {
                    int count = 0, total = 0;
                    for (int y = zy * zoneH; y < Math.Min((zy + 1) * zoneH, height); y++)
                    {
                        for (int x = xStart + zx * zoneW; x < Math.Min(xStart + (zx + 1) * zoneW, xEnd + 1); x++)
                        {
                            total++;
                            if (binary[x, y]) count++;
                        }
                    }
                    zones[zx, zy] = total > 0 ? (double)count / total : 0;
                }
            }

            // Feature: horizontal symmetry, vertical density distribution, holes
            double topDensity = (zones[0, 0] + zones[1, 0] + zones[2, 0]) / 3;
            double midDensity = (zones[0, 2] + zones[1, 2] + zones[2, 2]) / 3;
            double botDensity = (zones[0, 4] + zones[1, 4] + zones[2, 4]) / 3;
            double leftDensity = (zones[0, 0] + zones[0, 1] + zones[0, 2] + zones[0, 3] + zones[0, 4]) / 5;
            double rightDensity = (zones[2, 0] + zones[2, 1] + zones[2, 2] + zones[2, 3] + zones[2, 4]) / 5;
            double centerDensity = (zones[1, 0] + zones[1, 1] + zones[1, 2] + zones[1, 3] + zones[1, 4]) / 5;
            double totalDensity = 0;
            for (int zy = 0; zy < 5; zy++)
                for (int zx = 0; zx < 3; zx++)
                    totalDensity += zones[zx, zy];
            totalDensity /= 15;

            // Simple heuristic-based digit classification
            // 1: very narrow, mostly center column
            if (totalDensity < 0.15 || (centerDensity > leftDensity * 2 && centerDensity > rightDensity * 2 && totalDensity < 0.3))
                return '1';

            // 0: high density everywhere, roughly symmetric
            if (totalDensity > 0.35 && Math.Abs(leftDensity - rightDensity) < 0.15 && midDensity < topDensity && midDensity < botDensity)
                return '0';

            // 8: high density everywhere, symmetric
            if (totalDensity > 0.4 && Math.Abs(topDensity - botDensity) < 0.15 && midDensity > 0.2)
                return '8';

            // 7: top heavy, right-leaning
            if (topDensity > 0.35 && botDensity < 0.2 && midDensity < 0.25)
                return '7';

            // 4: mid-heavy with left component
            if (midDensity > topDensity && midDensity > botDensity && rightDensity > leftDensity)
                return '4';

            // 2: top and bottom heavy, mid thin
            if (topDensity > 0.2 && botDensity > 0.3 && zones[0, 2] < 0.2)
                return '2';

            // 3: right-heavy with horizontal strokes
            if (rightDensity > leftDensity && topDensity > 0.2 && midDensity > 0.2 && botDensity > 0.2)
                return '3';

            // 5: top-left and bottom-right pattern
            if (topDensity > 0.2 && zones[0, 1] > 0.2 && zones[2, 3] > 0.2 && botDensity > 0.2)
                return '5';

            // 6: top thin, bottom heavy with loop
            if (botDensity > topDensity && zones[0, 2] > 0.2 && zones[0, 3] > 0.2)
                return '6';

            // 9: top heavy with loop, bottom thin
            if (topDensity > botDensity && zones[2, 1] > 0.2 && zones[2, 2] > 0.2)
                return '9';

            // Fallback: use relative density
            var scores = new Dictionary<char, double>
            {
                ['0'] = (topDensity + botDensity) * 0.5 + (1 - midDensity) * 0.3,
                ['1'] = (1 - totalDensity) * 0.5 + centerDensity * 0.3,
                ['2'] = topDensity * 0.3 + botDensity * 0.3 + (1 - zones[0, 2]) * 0.2,
                ['3'] = rightDensity * 0.3 + midDensity * 0.2 + topDensity * 0.2,
                ['4'] = midDensity * 0.4 + rightDensity * 0.3,
                ['5'] = zones[0, 1] * 0.3 + zones[2, 3] * 0.3 + botDensity * 0.2,
                ['6'] = botDensity * 0.4 + leftDensity * 0.3,
                ['7'] = topDensity * 0.5 + (1 - botDensity) * 0.3,
                ['8'] = totalDensity * 0.5 + midDensity * 0.3,
                ['9'] = topDensity * 0.4 + rightDensity * 0.3
            };

            return scores.OrderByDescending(kv => kv.Value).First().Key;
        }

        #endregion

        #region Unified IPO List & NSE

        /// <summary>
        /// Scrapes all registrars in parallel and returns a unified list tagged with registrar name.
        /// Uses database cache as fallback for any registrar that fails.
        /// </summary>
        public async Task<ReturnData<List<IPOAllotmentCompany>>> GetAllIPOsAsync()
        {
            try
            {
                // Step 1: Run all scrapes in parallel (HTTP only — no DB writes here)
                var registrarScrapers = new Dictionary<string, Func<Task<List<IPOAllotmentCompany>>>>
                {
                    ["Linkin"] = GetIPOsFromMUFGIntimeAsync,
                    ["KFinTech"] = GetIPOsFromKFinTechAsync,
                    ["BigShare"] = GetIPOsFromBigShareAsync,
                    ["Purva"] = GetIPOsFromPurvaAsync,
                    ["SkyLine"] = GetIPOsFromSkylineAsync,
                    ["Integrated"] = GetIPOsFromIntegratedAsync,
                    ["Maashitla"] = GetIPOsFromMaashitlaAsync,
                    ["Cambridge"] = GetIPOsFromCambridgeAsync
                };

                // Execute all scrapes in parallel and capture results + errors
                var scrapeResults = new Dictionary<string, (List<IPOAllotmentCompany> ipos, string? error)>();
                var tasks = registrarScrapers.Select(async kv =>
                {
                    try
                    {
                        var ipos = await kv.Value();
                        return (Registrar: kv.Key, Ipos: ipos, Error: (string?)null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Live scrape failed for {Registrar} in GetAllIPOs", kv.Key);
                        return (Registrar: kv.Key, Ipos: new List<IPOAllotmentCompany>(), Error: ex.Message);
                    }
                }).ToList();

                var results = await Task.WhenAll(tasks);

                // Step 2: Process results sequentially (DB writes must be sequential for DbContext thread safety)
                var allIpos = new List<IPOAllotmentCompany>();
                var registrarCounts = new List<string>();
                var cachedRegistrars = new List<string>();

                foreach (var result in results)
                {
                    var ipos = result.Ipos;

                    if (ipos.Count > 0)
                    {
                        // Live scrape succeeded — update cache
                        await UpdateCacheAsync(result.Registrar, ipos);
                    }
                    else
                    {
                        // Live scrape failed or empty — try cache
                        var (cachedIpos, _) = await LoadFromCacheAsync(result.Registrar,
                            result.Error ?? "Live scrape returned 0 results");

                        if (cachedIpos != null && cachedIpos.Count > 0)
                        {
                            ipos = cachedIpos;
                            cachedRegistrars.Add(result.Registrar);
                            _logger.LogInformation("{Registrar}: Serving {Count} IPOs from cache", result.Registrar, ipos.Count);
                        }
                    }

                    // Tag with registrar name
                    foreach (var ipo in ipos)
                    {
                        if (string.IsNullOrEmpty(ipo.Registrar))
                            ipo.Registrar = result.Registrar;
                    }
                    allIpos.AddRange(ipos);

                    if (ipos.Count > 0)
                        registrarCounts.Add($"{result.Registrar}: {ipos.Count}");
                }

                var summary = registrarCounts.Count > 0
                    ? $"Found {allIpos.Count} IPOs across {registrarCounts.Count} registrars ({string.Join(", ", registrarCounts)})"
                    : "No IPOs found from any registrar";

                if (cachedRegistrars.Count > 0)
                    summary += $". Cached data used for: {string.Join(", ", cachedRegistrars)}";

                _logger.LogInformation(summary);

                if (cachedRegistrars.Count > 0)
                    return ReturnData<List<IPOAllotmentCompany>>.WarningResponse(summary, 206, allIpos);

                return ReturnData<List<IPOAllotmentCompany>>.SuccessResponse(allIpos, summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch IPOs from all registrars");
                return ReturnData<List<IPOAllotmentCompany>>.ErrorResponse($"Failed to fetch IPOs: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Fetches current and upcoming IPOs from NSE India API
        /// </summary>
        public async Task<ReturnData<List<IPOAllotmentCompany>>> GetIPOsFromNSEAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Registrar");
                var allIpos = new List<IPOAllotmentCompany>();

                // Fetch current + upcoming from NSE
                var urls = new[]
                {
                    "https://www.nseindia.com/api/ipo-current-issue",
                    "https://www.nseindia.com/api/all-upcoming-issues?category=ipo"
                };

                foreach (var url in urls)
                {
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                        var response = await client.SendAsync(request);
                        if (!response.IsSuccessStatusCode) continue;

                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);

                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in doc.RootElement.EnumerateArray())
                            {
                                var symbol = item.TryGetProperty("symbol", out var symProp) ? symProp.GetString() : "";
                                var name = item.TryGetProperty("companyName", out var nameProp) ? nameProp.GetString() : "";
                                var series = item.TryGetProperty("series", out var serProp) ? serProp.GetString() : "";
                                var status = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "";
                                var issueStart = item.TryGetProperty("issueStartDate", out var startProp) ? startProp.GetString() : "";
                                var issueEnd = item.TryGetProperty("issueEndDate", out var endProp) ? endProp.GetString() : "";
                                var issuePrice = item.TryGetProperty("issuePrice", out var priceProp) ? priceProp.GetString() : "";

                                if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(name))
                                    continue;

                                // Skip debt issues - only equity IPOs
                                if (series?.Equals("DEBT", StringComparison.OrdinalIgnoreCase) == true)
                                    continue;

                                // Check if already added (dedup by symbol)
                                if (allIpos.Any(i => i.CompanyCode.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
                                    continue;

                                allIpos.Add(new IPOAllotmentCompany
                                {
                                    CompanyCode = symbol!,
                                    CompanyName = $"{name} [{status}] [{issueStart} - {issueEnd}] {issuePrice}".Trim(),
                                    Registrar = "NSE"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch from NSE endpoint: {Url}", url);
                    }
                }

                _logger.LogInformation("NSE: Found {Count} IPOs", allIpos.Count);

                if (allIpos.Count > 0)
                    return ReturnData<List<IPOAllotmentCompany>>.SuccessResponse(allIpos, $"Found {allIpos.Count} IPOs from NSE");

                return ReturnData<List<IPOAllotmentCompany>>.WarningResponse("No equity IPOs found from NSE at this time");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch IPOs from NSE");
                return ReturnData<List<IPOAllotmentCompany>>.ErrorResponse($"Failed to fetch from NSE: {ex.Message}", 500);
            }
        }

        #endregion

        #region Cache Helpers

        /// <summary>
        /// Updates the cache row for a registrar with fresh data (upsert: insert if not exists, update if exists).
        /// </summary>
        private async Task UpdateCacheAsync(string registrarName, List<IPOAllotmentCompany> ipos)
        {
            try
            {
                var json = JsonSerializer.Serialize(ipos);
                var cacheRow = await _dbContext.IPO_RegistrarCache
                    .FirstOrDefaultAsync(c => c.RegistrarName == registrarName);

                if (cacheRow == null)
                {
                    cacheRow = new Models.Entities.IPO_RegistrarCache
                    {
                        RegistrarName = registrarName,
                        CachedIposJson = json,
                        CachedIpoCount = ipos.Count,
                        LastFetchedAt = DateTime.UtcNow,
                        LastErrorMessage = null,
                        LastFailedAt = null
                    };
                    _dbContext.IPO_RegistrarCache.Add(cacheRow);
                }
                else
                {
                    cacheRow.CachedIposJson = json;
                    cacheRow.CachedIpoCount = ipos.Count;
                    cacheRow.LastFetchedAt = DateTime.UtcNow;
                    cacheRow.LastErrorMessage = null;
                    cacheRow.LastFailedAt = null;
                    cacheRow.ModifiedDate = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("Cache updated for {Registrar}: {Count} IPOs", registrarName, ipos.Count);
            }
            catch (Exception ex)
            {
                // Cache update failure should not break the main flow
                _logger.LogWarning(ex, "Failed to update cache for {Registrar}", registrarName);
            }
        }

        /// <summary>
        /// Loads cached IPO list from the database for a given registrar.
        /// Records the current error in the cache row for observability.
        /// Returns null if no cache exists.
        /// </summary>
        private async Task<(List<IPOAllotmentCompany>? Ipos, DateTime? LastFetchedAt)> LoadFromCacheAsync(
            string registrarName, string? errorMessage = null)
        {
            try
            {
                var cacheRow = await _dbContext.IPO_RegistrarCache
                    .FirstOrDefaultAsync(c => c.RegistrarName == registrarName && c.IsActive);

                if (cacheRow == null || string.IsNullOrWhiteSpace(cacheRow.CachedIposJson))
                    return (null, null);

                // Record the failure for observability
                cacheRow.LastFailedAt = DateTime.UtcNow;
                cacheRow.LastErrorMessage = errorMessage?.Length > 1000
                    ? errorMessage[..1000]
                    : errorMessage;
                cacheRow.ModifiedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                var ipos = JsonSerializer.Deserialize<List<IPOAllotmentCompany>>(cacheRow.CachedIposJson);
                return (ipos, cacheRow.LastFetchedAt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load cache for {Registrar}", registrarName);
                return (null, null);
            }
        }

        /// <summary>
        /// Returns a human-readable age string from a TimeSpan.
        /// </summary>
        private static string FormatCacheAge(TimeSpan age)
        {
            if (age.TotalMinutes < 1) return "just now";
            if (age.TotalHours < 1) return $"{(int)age.TotalMinutes} minutes";
            if (age.TotalDays < 1) return $"{(int)age.TotalHours} hours";
            return $"{(int)age.TotalDays} days";
        }

        #endregion
    }
}
