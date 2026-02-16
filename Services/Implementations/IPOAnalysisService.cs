using System.Text.Json;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class IPOAnalysisService : IIPOAnalysisService
    {
        private readonly IIPOAnalysisRepository _analysisRepository;

        // Use enum-based keys for investor types
        private static readonly IPOInvestorType[] InvestorTypes = { IPOInvestorType.Retail, IPOInvestorType.SHNI, IPOInvestorType.BHNI };

        public IPOAnalysisService(IIPOAnalysisRepository analysisRepository)
        {
            _analysisRepository = analysisRepository;
        }

        public async Task<ReturnData<IPOAnalysisResponse>> CalculateAnalysisAsync(IPOAnalysisRequest request, int companyId)
        {
            try
            {
                var ipoMaster = await _analysisRepository.GetIPOMasterAsync(request.IPOId, companyId);
                if (ipoMaster == null)
                    return ReturnData<IPOAnalysisResponse>.ErrorResponse("IPO not found", 404);

                // Get order count data (same as OrderStatusSummary)
                var orderSummary = await _analysisRepository.GetOrderStatusSummaryAsync(request.IPOId, companyId);

                // Get share qty data
                ShareQtyData shareQtyData;
                if (request.AnalysisType == 1)
                    shareQtyData = await _analysisRepository.GetShareQtyDataAsync(request.IPOId, companyId);
                else
                    shareQtyData = await _analysisRepository.GetAllotedShareQtyDataAsync(request.IPOId, companyId);

                var response = new IPOAnalysisResponse
                {
                    AnalysisType = request.AnalysisType,
                    IPOPricePerShare = ipoMaster.IPO_Upper_Price_Band,
                    // Include input values in response for form pre-fill
                    ExpectedApplications_Retail = request.ExpectedApplications_Retail ?? 0,
                    ExpectedApplications_SHNI = request.ExpectedApplications_SHNI ?? 0,
                    ExpectedApplications_BHNI = request.ExpectedApplications_BHNI ?? 0,
                    ActualAllottedQty_Total = request.ActualAllottedQty_Total ?? 0,
                    ActualAllottedQty_Retail = request.ActualAllottedQty_Retail ?? 0,
                    ActualAllottedQty_SHNI = request.ActualAllottedQty_SHNI ?? 0,
                    ActualAllottedQty_BHNI = request.ActualAllottedQty_BHNI ?? 0,
                    ProfitMargin = request.ProfitMargin ?? 0,
                    SpotPremium = request.SpotPremium ?? 0,
                    SpotPrice = request.SpotPrice
                };

                // Populate read-only actual allotted qty from DB for Tab 2/3
                if (request.AnalysisType >= 2)
                {
                    var allottedSummary = await _analysisRepository.GetActualAllottedQtySummaryAsync(request.IPOId, companyId);
                    response.DbActualAllottedQty_Total = allottedSummary.Total;
                    response.DbActualAllottedQty_Retail = allottedSummary.Retail;
                    response.DbActualAllottedQty_SHNI = allottedSummary.SHNI;
                    response.DbActualAllottedQty_BHNI = allottedSummary.BHNI;
                }

                // Copy count tables from order summary
                response.KostakCount = orderSummary.Kostak;
                response.SubjectToCount = orderSummary.SubjectTo;
                response.PremiumCount = orderSummary.Premium;

                // Build share qty tables
                BuildShareQtyTables(response, shareQtyData, request, ipoMaster);

                // Build subscription data
                BuildSubscriptions(response, ipoMaster, request, orderSummary);

                // Build rates
                BuildRates(response, orderSummary, request);

                // Calculate Difference Qty (To Hedge)
                response.DifferenceQtyToHedge = CalculateDifferenceQty(response);

                // Tab 3: Profit or Loss
                if (request.AnalysisType == 3 && request.SpotPrice.HasValue)
                {
                    response.ProfitOrLoss = CalculateProfitOrLoss(response, request.SpotPrice.Value, ipoMaster.IPO_Upper_Price_Band);
                    // Rates show null for After Listing
                    response.KostakRates = new Dictionary<string, RateBlock>();
                    response.SubjectToRates = new Dictionary<string, RateBlock>();
                }

                return ReturnData<IPOAnalysisResponse>.SuccessResponse(response, "Analysis calculated successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<IPOAnalysisResponse>.ErrorResponse($"Error calculating analysis: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<IPOAnalysisResponse>> SubmitAnalysisAsync(IPOAnalysisRequest request, int userId, int companyId)
        {
            try
            {
                var calcResult = await CalculateAnalysisAsync(request, companyId);
                if (!calcResult.Success || calcResult.Data == null)
                    return calcResult;

                var resultJson = JsonSerializer.Serialize(calcResult.Data);

                var analysis = new IPO_Analysis
                {
                    IPOId = request.IPOId,
                    AnalysisType = request.AnalysisType,
                    ExpectedApplications_Retail = request.ExpectedApplications_Retail,
                    ExpectedApplications_SHNI = request.ExpectedApplications_SHNI,
                    ExpectedApplications_BHNI = request.ExpectedApplications_BHNI,
                    ActualAllottedQty_Total = request.ActualAllottedQty_Total,
                    ActualAllottedQty_Retail = request.ActualAllottedQty_Retail,
                    ActualAllottedQty_SHNI = request.ActualAllottedQty_SHNI,
                    ActualAllottedQty_BHNI = request.ActualAllottedQty_BHNI,
                    ProfitMargin = request.ProfitMargin,
                    SpotPremium = request.SpotPremium,
                    SpotPrice = request.SpotPrice,
                    CalculatedResultJson = resultJson,
                    CompanyId = companyId,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };

                await _analysisRepository.UpsertAnalysisAsync(analysis);

                return ReturnData<IPOAnalysisResponse>.SuccessResponse(calcResult.Data, "Analysis submitted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<IPOAnalysisResponse>.ErrorResponse($"Error submitting analysis: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<IPOAnalysisResponse>> GetAnalysisAsync(int ipoId, int analysisType, int companyId)
        {
            try
            {
                var analysis = await _analysisRepository.GetAnalysisAsync(ipoId, analysisType, companyId);

                // If saved analysis exists, return it with stored input values
                if (analysis != null && !string.IsNullOrEmpty(analysis.CalculatedResultJson))
                {
                    var response = JsonSerializer.Deserialize<IPOAnalysisResponse>(analysis.CalculatedResultJson);
                    if (response != null)
                    {
                        // Populate input fields from the stored entity (in case JSON doesn't have them)
                        response.ExpectedApplications_Retail = analysis.ExpectedApplications_Retail ?? 0;
                        response.ExpectedApplications_SHNI = analysis.ExpectedApplications_SHNI ?? 0;
                        response.ExpectedApplications_BHNI = analysis.ExpectedApplications_BHNI ?? 0;
                        response.ActualAllottedQty_Total = analysis.ActualAllottedQty_Total ?? 0;
                        response.ActualAllottedQty_Retail = analysis.ActualAllottedQty_Retail ?? 0;
                        response.ActualAllottedQty_SHNI = analysis.ActualAllottedQty_SHNI ?? 0;
                        response.ActualAllottedQty_BHNI = analysis.ActualAllottedQty_BHNI ?? 0;
                        response.ProfitMargin = analysis.ProfitMargin ?? 0;
                        response.SpotPremium = analysis.SpotPremium ?? 0;
                        response.SpotPrice = analysis.SpotPrice;

                        // Always populate live DbActualAllottedQty from DB for Type 2/3
                        if (analysisType >= 2)
                        {
                            var allottedSummary = await _analysisRepository.GetActualAllottedQtySummaryAsync(ipoId, companyId);
                            response.DbActualAllottedQty_Total = allottedSummary.Total;
                            response.DbActualAllottedQty_Retail = allottedSummary.Retail;
                            response.DbActualAllottedQty_SHNI = allottedSummary.SHNI;
                            response.DbActualAllottedQty_BHNI = allottedSummary.BHNI;
                        }
                    }
                    return ReturnData<IPOAnalysisResponse>.SuccessResponse(response!, "Analysis retrieved successfully", 200);
                }

                // No saved analysis — calculate fresh result with default inputs
                var request = new IPOAnalysisRequest
                {
                    IPOId = ipoId,
                    AnalysisType = analysisType
                };

                return await CalculateAnalysisAsync(request, companyId);
            }
            catch (Exception ex)
            {
                return ReturnData<IPOAnalysisResponse>.ErrorResponse($"Error retrieving analysis: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<List<IPOAnalysisResponse>>> GetAllAnalysesAsync(int ipoId, int companyId)
        {
            try
            {
                var analyses = await _analysisRepository.GetAllAnalysesAsync(ipoId, companyId);
                var responses = new List<IPOAnalysisResponse>();

                // If saved analyses exist, return them with stored input values
                if (analyses.Any())
                {
                    foreach (var a in analyses)
                    {
                        if (!string.IsNullOrEmpty(a.CalculatedResultJson))
                        {
                            var r = JsonSerializer.Deserialize<IPOAnalysisResponse>(a.CalculatedResultJson);
                            if (r != null)
                            {
                                // Populate input fields from the stored entity (in case JSON doesn't have them)
                                r.ExpectedApplications_Retail = a.ExpectedApplications_Retail ?? 0;
                                r.ExpectedApplications_SHNI = a.ExpectedApplications_SHNI ?? 0;
                                r.ExpectedApplications_BHNI = a.ExpectedApplications_BHNI ?? 0;
                                r.ActualAllottedQty_Total = a.ActualAllottedQty_Total ?? 0;
                                r.ActualAllottedQty_Retail = a.ActualAllottedQty_Retail ?? 0;
                                r.ActualAllottedQty_SHNI = a.ActualAllottedQty_SHNI ?? 0;
                                r.ActualAllottedQty_BHNI = a.ActualAllottedQty_BHNI ?? 0;
                                r.ProfitMargin = a.ProfitMargin ?? 0;
                                r.SpotPremium = a.SpotPremium ?? 0;
                                r.SpotPrice = a.SpotPrice;

                                // Always populate live DbActualAllottedQty from DB for Type 2/3
                                if (a.AnalysisType >= 2)
                                {
                                    var allottedSummary = await _analysisRepository.GetActualAllottedQtySummaryAsync(ipoId, companyId);
                                    r.DbActualAllottedQty_Total = allottedSummary.Total;
                                    r.DbActualAllottedQty_Retail = allottedSummary.Retail;
                                    r.DbActualAllottedQty_SHNI = allottedSummary.SHNI;
                                    r.DbActualAllottedQty_BHNI = allottedSummary.BHNI;
                                }

                                responses.Add(r);
                            }
                        }
                    }
                }
                else
                {
                    // No saved analyses - calculate fresh for all 3 analysis types with default inputs
                    for (int analysisType = 0; analysisType <= 2; analysisType++)
                    {
                        var request = new IPOAnalysisRequest
                        {
                            IPOId = ipoId,
                            AnalysisType = analysisType,
                            ExpectedApplications_Retail = 0,
                            ExpectedApplications_SHNI = 0,
                            ExpectedApplications_BHNI = 0,
                            ActualAllottedQty_Total = 0,
                            ActualAllottedQty_Retail = 0,
                            ActualAllottedQty_SHNI = 0,
                            ActualAllottedQty_BHNI = 0,
                            ProfitMargin = 0,
                            SpotPremium = 0,
                            SpotPrice = 0
                        };

                        var calcResult = await CalculateAnalysisAsync(request, companyId);
                        if (calcResult.Success && calcResult.Data != null)
                        {
                            responses.Add(calcResult.Data);
                        }
                    }
                }

                return ReturnData<List<IPOAnalysisResponse>>.SuccessResponse(responses, "Analyses retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<List<IPOAnalysisResponse>>.ErrorResponse($"Error retrieving analyses: {ex.Message}", 500);
            }
        }

        #region Private Calculation Methods

        private void BuildShareQtyTables(IPOAnalysisResponse response, ShareQtyData data, IPOAnalysisRequest request, IPO_IPOMaster ipo)
        {
            // Initialize using enum keys
            foreach (var t in InvestorTypes)
            {
                var key = t.ToString();
                response.KostakShareQty[key] = new CategoryStatusBlock();
                response.SubjectToShareQty[key] = new CategoryStatusBlock();
            }

            foreach (var row in data.Rows)
            {
                var investorKey = ((IPOInvestorType)row.InvestorType).ToString();
                var isBuy = row.OrderType == (int)IPOOrderType.BUY;

                if (row.OrderCategory == (int)IPOOrderCategory.Kostak)
                {
                    if (!response.KostakShareQty.ContainsKey(investorKey))
                        response.KostakShareQty[investorKey] = new CategoryStatusBlock();
                    var target = isBuy ? response.KostakShareQty[investorKey].Buy : response.KostakShareQty[investorKey].Sell;
                    target.Count += row.TotalQty;
                }
                else if (row.OrderCategory == (int)IPOOrderCategory.SubjectTo)
                {
                    if (!response.SubjectToShareQty.ContainsKey(investorKey))
                        response.SubjectToShareQty[investorKey] = new CategoryStatusBlock();
                    var target = isBuy ? response.SubjectToShareQty[investorKey].Buy : response.SubjectToShareQty[investorKey].Sell;
                    target.Count += row.TotalQty;
                }
                else if (row.OrderCategory == (int)IPOOrderCategory.Premium)
                {
                    var target = isBuy ? response.PremiumShareQty.Buy : response.PremiumShareQty.Sell;
                    target.Count += row.TotalQty;
                }
            }

            // For Tab 2/3: multiply share qty by actual allotted qty per investor type
            if (request.AnalysisType >= 2)
            {
                var allotmentMap = new Dictionary<IPOInvestorType, decimal>
                {
                    [IPOInvestorType.Retail] = request.ActualAllottedQty_Retail ?? 0,
                    [IPOInvestorType.SHNI] = request.ActualAllottedQty_SHNI ?? 0,
                    [IPOInvestorType.BHNI] = request.ActualAllottedQty_BHNI ?? 0
                };

                foreach (var t in InvestorTypes)
                {
                    var key = t.ToString();
                    var allotment = allotmentMap[t];

                    if (response.KostakShareQty.ContainsKey(key) && response.KostakCount.ContainsKey(key))
                    {
                        var block = response.KostakShareQty[key];
                        block.Buy.Count = (int)(response.KostakCount[key].Buy.Count * allotment);
                        block.Sell.Count = (int)(response.KostakCount[key].Sell.Count * allotment);
                    }
                    if (response.SubjectToShareQty.ContainsKey(key) && response.SubjectToCount.ContainsKey(key))
                    {
                        var block = response.SubjectToShareQty[key];
                        block.Buy.Count = (int)(response.SubjectToCount[key].Buy.Count * allotment);
                        block.Sell.Count = (int)(response.SubjectToCount[key].Sell.Count * allotment);
                    }
                }

                var totalAllotment = request.ActualAllottedQty_Total ?? 0;
                response.PremiumShareQty.Buy.Count = (int)(response.PremiumCount.Buy.Count * totalAllotment);
                response.PremiumShareQty.Sell.Count = (int)(response.PremiumCount.Sell.Count * totalAllotment);
            }

            // NET calculation for share qty
            foreach (var t in InvestorTypes)
            {
                var key = t.ToString();
                if (response.KostakShareQty.ContainsKey(key))
                {
                    var b = response.KostakShareQty[key];
                    b.Net.Count = b.Buy.Count - b.Sell.Count;
                }
                if (response.SubjectToShareQty.ContainsKey(key))
                {
                    var b = response.SubjectToShareQty[key];
                    b.Net.Count = b.Buy.Count - b.Sell.Count;
                }
            }
            response.PremiumShareQty.Net.Count = response.PremiumShareQty.Buy.Count - response.PremiumShareQty.Sell.Count;
        }

        private void BuildSubscriptions(IPOAnalysisResponse response, IPO_IPOMaster ipo, IPOAnalysisRequest request, OrderStatusSummaryResponse orderSummary)
        {
            var ipoPrice = ipo.IPO_Upper_Price_Band;
            var totalIPOSizeCr = ipo.Total_IPO_Size_Cr;
            var totalShares = ipoPrice > 0 ? (totalIPOSizeCr * 10000000m) / ipoPrice : 0;

            var types = new[]
            {
                (IPOInvestorType.Retail, (decimal)ipo.Retail_Percentage, request.ExpectedApplications_Retail, request.ActualAllottedQty_Retail),
                (IPOInvestorType.SHNI, (decimal)(ipo.SHNI_Percentage ?? 0), request.ExpectedApplications_SHNI, request.ActualAllottedQty_SHNI),
                (IPOInvestorType.BHNI, (decimal)(ipo.BHNI_Percentage ?? 0), request.ExpectedApplications_BHNI, request.ActualAllottedQty_BHNI)
            };

            foreach (var (investorType, percentage, expectedApps, actualAllotment) in types)
            {
                var typeName = investorType.ToString();
                var sharesForType = totalShares * percentage / 100m;

                // ApplicationForOneTime = total BUY count across Kostak+SubjectTo for this investor type
                var kostakBuyCount = orderSummary.Kostak.ContainsKey(typeName) ? orderSummary.Kostak[typeName].Buy.Count : 0;
                var subjectToBuyCount = orderSummary.SubjectTo.ContainsKey(typeName) ? orderSummary.SubjectTo[typeName].Buy.Count : 0;
                var appForOneTime = kostakBuyCount + subjectToBuyCount;

                var sub = new SubscriptionBlock
                {
                    ApplicationForOneTime = appForOneTime
                };

                if (request.AnalysisType == 1)
                {
                    var expected = expectedApps ?? 0;
                    sub.ExpectedApplications = expected;
                    sub.AvgSharePerApplication = expected > 0 ? Math.Round(sharesForType / expected, 1) : 0;
                    sub.IPOSubscription = appForOneTime > 0 ? Math.Round(expected / appForOneTime, 1) : 0;
                }
                else
                {
                    var allotted = actualAllotment ?? 0;
                    sub.ExpectedApplications = allotted;
                    sub.AvgSharePerApplication = allotted;
                    sub.IPOSubscription = appForOneTime > 0 && allotted > 0
                        ? Math.Round(sharesForType / (appForOneTime * allotted), 1) : 0;
                }

                response.Subscriptions[typeName] = sub;
            }
        }

        private void BuildRates(IPOAnalysisResponse response, OrderStatusSummaryResponse orderSummary, IPOAnalysisRequest request)
        {
            if (request.AnalysisType == 3)
                return; // No rates for After Listing

            var profitMargin = request.ProfitMargin ?? 0;

            foreach (var t in InvestorTypes)
            {
                var key = t.ToString();

                // Kostak rates
                var kostakAvg = orderSummary.Kostak.ContainsKey(key)
                    ? orderSummary.Kostak[key].Net.Avg : 0;
                response.KostakRates[key] = new RateBlock
                {
                    WithoutProfitMargin = Math.Round(kostakAvg, 2),
                    WithProfitMargin = Math.Round(kostakAvg * (1 + profitMargin / 100m), 2)
                };

                // SubjectTo rates
                var subjectAvg = orderSummary.SubjectTo.ContainsKey(key)
                    ? orderSummary.SubjectTo[key].Net.Avg : 0;
                response.SubjectToRates[key] = new RateBlock
                {
                    WithoutProfitMargin = Math.Round(subjectAvg, 2),
                    WithProfitMargin = Math.Round(subjectAvg * (1 + profitMargin / 100m), 2)
                };
            }
        }

        private decimal CalculateDifferenceQty(IPOAnalysisResponse response)
        {
            decimal total = 0;
            foreach (var kvp in response.KostakShareQty)
                total += kvp.Value.Net.Count;
            foreach (var kvp in response.SubjectToShareQty)
                total += kvp.Value.Net.Count;
            total += response.PremiumShareQty.Net.Count;
            return total;
        }

        private decimal CalculateProfitOrLoss(IPOAnalysisResponse response, decimal spotPrice, decimal ipoPrice)
        {
            var sharePnL = response.DifferenceQtyToHedge * (spotPrice - ipoPrice);

            decimal netKostakAmount = 0;
            foreach (var kvp in response.KostakCount)
                netKostakAmount += kvp.Value.Net.Amount;

            decimal netSubjectToAmount = 0;
            foreach (var kvp in response.SubjectToCount)
                netSubjectToAmount += kvp.Value.Net.Amount;

            var netPremiumAmount = response.PremiumCount.Net.Amount;

            return Math.Round(sharePnL + netKostakAmount + netSubjectToAmount + netPremiumAmount, 2);
        }

        #endregion
    }
}
