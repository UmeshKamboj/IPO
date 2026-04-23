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
        private readonly IIPORepository _ipoRepository;
        private readonly IIPOBuyerPlaceOrderRepository _buyerPlaceOrderRepository;

        // Use enum-based keys for investor types
        private static readonly IPOInvestorType[] InvestorTypes = { IPOInvestorType.Retail, IPOInvestorType.SHNI, IPOInvestorType.BHNI };

        public IPOAnalysisService(
            IIPOAnalysisRepository analysisRepository,
            IIPORepository ipoRepository,
            IIPOBuyerPlaceOrderRepository buyerPlaceOrderRepository)
        {
            _analysisRepository = analysisRepository;
            _ipoRepository = ipoRepository;
            _buyerPlaceOrderRepository = buyerPlaceOrderRepository;
        }

        public async Task<ReturnData<IPOAnalysisResponse>> CalculateAnalysisAsync(IPOAnalysisRequest request, int companyId)
        {
            try
            {
                var ipoMaster = await _analysisRepository.GetIPOMasterAsync(request.IPOId, companyId);
                if (ipoMaster == null)
                    return ReturnData<IPOAnalysisResponse>.ErrorResponse("IPO not found", 404);

                // Run all independent queries in parallel — significant speedup vs sequential awaits
                var orderSummaryTask = _analysisRepository.GetOrderStatusSummaryAsync(request.IPOId, companyId, ipoMaster,
                    request.AnalysisType == 3 ? request.SpotPrice : null);

                var shareQtyTask = request.AnalysisType == 1
                    ? _analysisRepository.GetShareQtyDataAsync(request.IPOId, companyId)
                    : _analysisRepository.GetAllotedShareQtyDataAsync(request.IPOId, companyId);

                var sharedFieldsTask = _analysisRepository.GetLatestSharedFieldsAsync(request.IPOId, companyId);

                var allottedSummaryTask = request.AnalysisType >= 2
                    ? _analysisRepository.GetActualAllottedQtySummaryAsync(request.IPOId, companyId)
                    : Task.FromResult(new ActualAllottedQtySummary());

                await Task.WhenAll(orderSummaryTask, shareQtyTask, sharedFieldsTask, allottedSummaryTask);

                var orderSummary = orderSummaryTask.Result;
                var shareQtyData = shareQtyTask.Result;
                var sharedFields = sharedFieldsTask.Result;

                var spotPrice = request.SpotPrice ?? sharedFields.SpotPrice ?? ipoMaster.OpenIPOPrice;
                var profitMargin = request.ProfitMargin ?? sharedFields.ProfitMargin ?? 0;
                var spotPremium = request.SpotPremium ?? sharedFields.SpotPremium ?? (spotPrice.HasValue
                    ? spotPrice.Value - ipoMaster.IPO_Upper_Price_Band
                    : 0);

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
                    ProfitMargin = profitMargin,
                    SpotPremium = spotPremium,
                    SpotPrice = spotPrice
                };

                // Populate read-only actual allotted qty from DB for Tab 2/3 (already fetched in parallel above)
                if (request.AnalysisType >= 2)
                {
                    var allottedSummary = allottedSummaryTask.Result;
                    response.DbActualAllottedQty_Total = allottedSummary.Total;
                    response.DbActualAllottedQty_Retail = allottedSummary.Retail;
                    response.DbActualAllottedQty_SHNI = allottedSummary.SHNI;
                    response.DbActualAllottedQty_BHNI = allottedSummary.BHNI;

                    // If user hasn't provided ActualAllottedQty, use DB values as fallback
                    if ((request.ActualAllottedQty_Total ?? 0) == 0)
                    {
                        request.ActualAllottedQty_Total = allottedSummary.Total;
                        response.ActualAllottedQty_Total = allottedSummary.Total;
                    }
                    if ((request.ActualAllottedQty_Retail ?? 0) == 0)
                    {
                        request.ActualAllottedQty_Retail = allottedSummary.Retail;
                        response.ActualAllottedQty_Retail = allottedSummary.Retail;
                    }
                    if ((request.ActualAllottedQty_SHNI ?? 0) == 0)
                    {
                        request.ActualAllottedQty_SHNI = allottedSummary.SHNI;
                        response.ActualAllottedQty_SHNI = allottedSummary.SHNI;
                    }
                    if ((request.ActualAllottedQty_BHNI ?? 0) == 0)
                    {
                        request.ActualAllottedQty_BHNI = allottedSummary.BHNI;
                        response.ActualAllottedQty_BHNI = allottedSummary.BHNI;
                    }
                }

                // Copy count tables from order summary
                response.KostakCount = orderSummary.Kostak;
                response.SubjectToCount = orderSummary.SubjectTo;
                response.PremiumCount = orderSummary.Premium;

                // Build share qty tables
                BuildShareQtyTables(response, shareQtyData, request, ipoMaster);

                // Build subscription data
                BuildSubscriptions(response, ipoMaster, request, orderSummary);

                // Tab 1: multiply Kostak/SubjectTo share qty by AvgSharePerApplication
                if (request.AnalysisType == 1)
                {
                    foreach (var t in InvestorTypes)
                    {
                        var key = t.ToString();
                        var avgSharePerApp = response.Subscriptions.ContainsKey(key)
                            ? response.Subscriptions[key].AvgSharePerApplication : 0;

                        if (response.KostakShareQty.ContainsKey(key))
                        {
                            var block = response.KostakShareQty[key];
                            block.Buy.Count = (int)Math.Round(response.KostakCount[key].Buy.Count * avgSharePerApp);
                            block.Sell.Count = (int)Math.Round(response.KostakCount[key].Sell.Count * avgSharePerApp);
                            block.Net.Count = block.Buy.Count - block.Sell.Count;
                        }
                        if (response.SubjectToShareQty.ContainsKey(key))
                        {
                            var block = response.SubjectToShareQty[key];
                            block.Buy.Count = (int)Math.Round(response.SubjectToCount[key].Buy.Count * avgSharePerApp);
                            block.Sell.Count = (int)Math.Round(response.SubjectToCount[key].Sell.Count * avgSharePerApp);
                            block.Net.Count = block.Buy.Count - block.Sell.Count;
                        }
                    }
                }

                // Build rates
                BuildRates(response, orderSummary, request);

                // Calculate Difference Qty
                var diffQty = CalculateDifferenceQty(response);
                response.DifferenceQtyToHedge = diffQty;

                // Tab 3: Difference Qty + Profit or Loss
                if (request.AnalysisType == 3)
                {
                    response.DifferenceQty = diffQty;

                    if (spotPrice.HasValue)
                    {
                        response.ProfitOrLoss = CalculateProfitOrLoss(response, spotPrice.Value, ipoMaster.IPO_Upper_Price_Band);
                    }

                    // Rates already initialized to 0 by BuildRates for Tab 3
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

                // Save with original request values — null/0 fields won't overwrite existing DB values
                // Treat 0 as null for shared fields so tabs don't clear each other
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
                    ProfitMargin = (request.AnalysisType == 1 || request.AnalysisType == 2) ? request.ProfitMargin : null,
                    SpotPremium = (request.AnalysisType == 1 || request.AnalysisType == 2) ? request.SpotPremium : null,
                    SpotPrice = request.AnalysisType == 3 ? request.SpotPrice : null,
                    CalculatedResultJson = resultJson,
                    CompanyId = companyId,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };

                await _analysisRepository.UpsertAnalysisAsync(analysis);

                // Sync only fields that belong to the current tab (use AnalysisType to determine)
                // Tab 1 & 2 have ProfitMargin, SpotPremium; Tab 3 has SpotPrice
                var profitMarginToSync = (request.AnalysisType == 1 || request.AnalysisType == 2) ? request.ProfitMargin : null;
                var spotPremiumToSync = (request.AnalysisType == 1 || request.AnalysisType == 2) ? request.SpotPremium : null;
                var spotPriceToSync = request.AnalysisType == 3 ? request.SpotPrice : null;
                await _analysisRepository.UpdateSharedFieldsAsync(
                    request.IPOId, companyId, profitMarginToSync, spotPremiumToSync, spotPriceToSync);

                // When SpotPrice is explicitly sent, update IPO Master and all child order PreOpenPrices
                if (request.SpotPrice.HasValue && request.SpotPrice.Value > 0)
                {
                    await _ipoRepository.UpdatePreOpenPriceAsync(request.IPOId, request.SpotPrice.Value, companyId, userId);
                    await _buyerPlaceOrderRepository.UpdateAllChildrenPreOpenPriceAsync(request.IPOId, request.SpotPrice.Value, companyId, userId);
                }

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

                // Re-calculate from saved inputs (or defaults) to ensure all computed fields are live
                var request = new IPOAnalysisRequest
                {
                    IPOId = ipoId,
                    AnalysisType = analysisType,
                    ExpectedApplications_Retail = analysis?.ExpectedApplications_Retail,
                    ExpectedApplications_SHNI = analysis?.ExpectedApplications_SHNI,
                    ExpectedApplications_BHNI = analysis?.ExpectedApplications_BHNI,
                    ActualAllottedQty_Total = analysis?.ActualAllottedQty_Total,
                    ActualAllottedQty_Retail = analysis?.ActualAllottedQty_Retail,
                    ActualAllottedQty_SHNI = analysis?.ActualAllottedQty_SHNI,
                    ActualAllottedQty_BHNI = analysis?.ActualAllottedQty_BHNI,
                    ProfitMargin = analysis?.ProfitMargin,
                    SpotPremium = analysis?.SpotPremium,
                    SpotPrice = analysis?.SpotPrice
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

                if (analyses.Any())
                {
                    // Re-calculate each saved analysis from stored inputs for live computed fields
                    foreach (var a in analyses)
                    {
                        var request = new IPOAnalysisRequest
                        {
                            IPOId = ipoId,
                            AnalysisType = a.AnalysisType,
                            ExpectedApplications_Retail = a.ExpectedApplications_Retail,
                            ExpectedApplications_SHNI = a.ExpectedApplications_SHNI,
                            ExpectedApplications_BHNI = a.ExpectedApplications_BHNI,
                            ActualAllottedQty_Total = a.ActualAllottedQty_Total,
                            ActualAllottedQty_Retail = a.ActualAllottedQty_Retail,
                            ActualAllottedQty_SHNI = a.ActualAllottedQty_SHNI,
                            ActualAllottedQty_BHNI = a.ActualAllottedQty_BHNI,
                            ProfitMargin = a.ProfitMargin,
                            SpotPremium = a.SpotPremium,
                            SpotPrice = a.SpotPrice
                        };

                        var calcResult = await CalculateAnalysisAsync(request, companyId);
                        if (calcResult.Success && calcResult.Data != null)
                            responses.Add(calcResult.Data);
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
                            AnalysisType = analysisType
                        };

                        var calcResult = await CalculateAnalysisAsync(request, companyId);
                        if (calcResult.Success && calcResult.Data != null)
                            responses.Add(calcResult.Data);
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
                    // Only Premium orders in share qty (CALL/PUT excluded, matching old app)
                    var target = isBuy ? response.PremiumShareQty.Buy : response.PremiumShareQty.Sell;
                    target.Count += row.TotalQty;
                }
            }

            // For Tab 2/3: Kostak/SubjectTo already have correct AllotedQty sums from GetAllotedShareQtyDataAsync
            // Only Premium needs override (AllotedQty=1 per child, so use order count directly)
            if (request.AnalysisType >= 2)
            {
                response.PremiumShareQty.Buy.Count = response.PremiumCount.Buy.Count;
                response.PremiumShareQty.Sell.Count = response.PremiumCount.Sell.Count;
            }
            else
            {
                // Tab 1 (Initial P&L): multiply count by lot-based allotment estimate
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
                (IPOInvestorType.Retail, (decimal)ipo.Retail_Percentage, request.ExpectedApplications_Retail, request.ActualAllottedQty_Retail, ipo.IPO_Retail_Lot_Size),
                (IPOInvestorType.SHNI, (decimal)(ipo.SHNI_Percentage ?? 0), request.ExpectedApplications_SHNI, request.ActualAllottedQty_SHNI, ipo.IPO_SHNI_Lot_Size ?? 0),
                (IPOInvestorType.BHNI, (decimal)(ipo.BHNI_Percentage ?? 0), request.ExpectedApplications_BHNI, request.ActualAllottedQty_BHNI, ipo.IPO_BHNI_Lot_Size ?? 0)
            };

            foreach (var (investorType, percentage, expectedApps, actualAllotment, lotSize) in types)
            {
                var typeName = investorType.ToString();
                var sharesForType = totalShares * percentage / 100m;

                // ApplicationForOneTime = total shares for this investor type / lot size
                var appForOneTime = lotSize > 0 ? (int)Math.Round(sharesForType / lotSize) : 0;

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
            var profitMargin = response.ProfitMargin;

            foreach (var t in InvestorTypes)
            {
                var key = t.ToString();

                // Kostak/Subject rates = 0 for all tabs (matching old app behavior)
                response.KostakRates[key] = new RateBlock { WithoutProfitMargin = 0, WithProfitMargin = 0 };
                response.SubjectToRates[key] = new RateBlock { WithoutProfitMargin = 0, WithProfitMargin = 0 };
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
            // P&L = sum of all billing amounts (Premium, CALL rate-only, PUT, Kostak)
            // No separate sharePnL — it's already embedded in Premium/PUT billing formulas
            // CALL uses rate-only so sharePnL is not double-counted

            decimal netKostakAmount = 0;
            foreach (var kvp in response.KostakCount)
                netKostakAmount += kvp.Value.Net.Amount;

            decimal netSubjectToAmount = 0;
            foreach (var kvp in response.SubjectToCount)
                netSubjectToAmount += kvp.Value.Net.Amount;

            var netPremiumAmount = response.PremiumCount.Net.Amount;

            return Math.Round(netKostakAmount + netSubjectToAmount + netPremiumAmount, 2);
        }

        #endregion
    }
}
