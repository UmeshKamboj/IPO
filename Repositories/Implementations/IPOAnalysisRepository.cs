using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class IPOAnalysisRepository : IIPOAnalysisRepository
    {
        private readonly IPOClientDbContext _context;

        public IPOAnalysisRepository(IPOClientDbContext context)
        {
            _context = context;
        }

        public async Task<IPO_IPOMaster?> GetIPOMasterAsync(int ipoId, int companyId)
        {
            return await _context.IPO_IPOMaster
                .FirstOrDefaultAsync(x => x.Id == ipoId && x.CompanyId == companyId && x.IsActive);
        }

        public async Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(int ipoId, int companyId, IPO_IPOMaster ipoMaster, decimal? overridePreOpenPrice = null)
        {
            var response = new OrderStatusSummaryResponse();

            // Use the already-fetched IPO Master — no extra DB round trip
            var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
            var ipoPreOpenPrice = overridePreOpenPrice ?? ipoMaster?.OpenIPOPrice ?? 0;

            // Get child orders with all required data
            var childOrders = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Where(c =>
                    c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                    c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                    c.IPOOrder.BuyerMaster.IsActive && !c.IPOOrder.BuyerMaster.IsDeleted && !c.IPOOrder.IsDeleted && !c.IsDeleted)
                .ToListAsync();

            // Group and calculate using new formula: (PreOpenPrice - IPOPrice) × AllotedQty - Rate
            // If AllotedQty is 0, Amount should be 0
            // Kostak/SubjectTo: per-child calculation
            var kostakSubjectRows = childOrders
                .Where(c => c.IPOOrder.OrderCategory == (int)IPOOrderCategory.Kostak ||
                             c.IPOOrder.OrderCategory == (int)IPOOrderCategory.SubjectTo)
                .GroupBy(c => new { c.IPOOrder.OrderCategory, c.IPOOrder.InvestorType, c.IPOOrder.OrderType })
                .Select(g =>
                {
                    var count = g.Count();
                    var totalAmount = g.Sum(c =>
                    {
                        var allotedQty = c.AllotedQty ?? 0;
                        var preOpenPrice = c.PreOpenPrice > 0 ? c.PreOpenPrice : ipoPreOpenPrice;
                        // If allotment was checked (PAN filled) but got 0 shares, amount = 0
                        // If no allotment check done (no PAN), still charge the rate: amount = -rate
                        decimal amount = (allotedQty == 0 && !string.IsNullOrEmpty(c.PANNumber))
                            ? 0
                            : (preOpenPrice - ipoPrice) * allotedQty - c.IPOOrder.Rate;
                        if (c.IPOOrder.OrderType == (int)IPOOrderType.SELL)
                            amount = -amount;
                        return amount;
                    });
                    return new { g.Key.OrderCategory, g.Key.InvestorType, g.Key.OrderType, Count = count, Avg = count > 0 ? totalAmount / count : 0, Amount = totalAmount };
                }).ToList();

            // Premium/CALL/PUT: per-order calculation using Order.Quantity
            var premOptRows = childOrders
                .Where(c => c.IPOOrder.OrderCategory == (int)IPOOrderCategory.Premium ||
                             c.IPOOrder.OrderCategory == (int)IPOOrderCategory.CALL ||
                             c.IPOOrder.OrderCategory == (int)IPOOrderCategory.PUT)
                .GroupBy(c => c.OrderId)
                .Select(g =>
                {
                    var first = g.First();
                    var order = first.IPOOrder;
                    var qty = order.Quantity;
                    // Always use the override/current PreOpenPrice for Analysis calculations
                    var preOpenPrice = ipoPreOpenPrice;
                    var orderCategory = order.OrderCategory;
                    // Use EffectiveRate (= what user entered) for Premium/CALL/PUT
                    var rate = order.EffectiveRate ?? order.Rate;

                    decimal amount;
                    if (orderCategory == (int)IPOOrderCategory.CALL)
                        amount = -rate * qty;
                    else if (orderCategory == (int)IPOOrderCategory.PUT)
                    {
                        decimal putSp = 0;
                        if (!string.IsNullOrEmpty(order.PremiumStrikePrice)
                            && decimal.TryParse(order.PremiumStrikePrice, out var parsedSp))
                            putSp = parsedSp;
                        amount = (ipoPrice - preOpenPrice - rate + putSp) * qty;
                    }
                    else // Premium
                        amount = (preOpenPrice - ipoPrice - rate) * qty;

                    if (order.OrderType == (int)IPOOrderType.SELL)
                        amount = -amount;

                    return new { orderCategory, order.InvestorType, order.OrderType, Qty = qty, Amount = amount };
                })
                .GroupBy(x => new { x.orderCategory, x.InvestorType, x.OrderType })
                .Select(g => new
                {
                    OrderCategory = g.Key.orderCategory,
                    g.Key.InvestorType,
                    g.Key.OrderType,
                    Count = g.Sum(x => x.Qty),
                    Avg = g.Sum(x => x.Qty) == 0 ? 0 : g.Sum(x => x.Amount) / g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.Amount)
                }).ToList();

            var grouped = kostakSubjectRows.Concat(premOptRows).ToList();

            // Pre-initialize
            var investorTypes = new[] { IPOInvestorType.Retail, IPOInvestorType.SHNI, IPOInvestorType.BHNI };
            foreach (var type in investorTypes)
            {
                var key = type.ToString();
                response.Kostak[key] = new CategoryStatusBlock();
                response.SubjectTo[key] = new CategoryStatusBlock();
            }

            // Kostak & SubjectTo
            foreach (var row in grouped.Where(x =>
                x.OrderCategory == (int)IPOOrderCategory.Kostak ||
                x.OrderCategory == (int)IPOOrderCategory.SubjectTo))
            {
                var dict = row.OrderCategory == (int)IPOOrderCategory.Kostak
                    ? response.Kostak : response.SubjectTo;

                var investorKey = ((IPOInvestorType)row.InvestorType).ToString();
                if (!dict.ContainsKey(investorKey))
                    dict[investorKey] = new CategoryStatusBlock();

                var block = dict[investorKey];
                var target = row.OrderType == (int)IPOOrderType.BUY ? block.Buy : block.Sell;

                target.Count += row.Count;
                target.Amount += row.Amount;
                target.Avg = target.Count == 0 ? 0 : target.Amount / target.Count;
            }

            // NET calculation
            void CalcNet(Dictionary<string, CategoryStatusBlock> dict)
            {
                foreach (var item in dict.Values)
                {
                    item.Net.Count = item.Buy.Count - item.Sell.Count;
                    // SELL amounts are already negated in the billing loop, so ADD (not subtract)
                    item.Net.Amount = item.Buy.Amount + item.Sell.Amount;
                    item.Net.Avg = item.Net.Count == 0 ? 0 : item.Net.Amount / item.Net.Count;
                }
            }
            CalcNet(response.Kostak);
            CalcNet(response.SubjectTo);

            // Premium: count only Premium orders, but include CALL/PUT amounts in P&L
            foreach (var row in grouped.Where(x =>
                x.OrderCategory == (int)IPOOrderCategory.Premium ||
                x.OrderCategory == (int)IPOOrderCategory.CALL ||
                x.OrderCategory == (int)IPOOrderCategory.PUT))
            {
                var target = row.OrderType == (int)IPOOrderType.BUY
                    ? response.Premium.Buy : response.Premium.Sell;
                // Only add count for Premium orders, not CALL/PUT
                if (row.OrderCategory == (int)IPOOrderCategory.Premium)
                    target.Count += row.Count;
                target.Amount += row.Amount;
                target.Avg = target.Count == 0 ? 0 : target.Amount / target.Count;
            }
            response.Premium.Net.Count = response.Premium.Buy.Count - response.Premium.Sell.Count;
            // SELL amounts are already negated in the billing loop, so ADD (not subtract)
            response.Premium.Net.Amount = response.Premium.Buy.Amount + response.Premium.Sell.Amount;
            response.Premium.Net.Avg = response.Premium.Net.Count == 0 ? 0
                : response.Premium.Net.Amount / response.Premium.Net.Count;

            return response;
        }

        /// <summary>
        /// Get share qty from child.Quantity (applications/children count)
        /// </summary>
        public async Task<ShareQtyData> GetShareQtyDataAsync(int ipoId, int companyId)
        {
            var rows = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder).ThenInclude(o => o.BuyerMaster)
                .Where(c =>
                    c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                    c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                    c.IPOOrder.BuyerMaster.IsActive &&
                    !c.IPOOrder.BuyerMaster.IsDeleted &&
                    !c.IPOOrder.IsDeleted &&
                    !c.IsDeleted)
                .GroupBy(c => new
                {
                    c.IPOOrder.OrderCategory,
                    c.IPOOrder.InvestorType,
                    c.IPOOrder.OrderType
                })
                .Select(g => new ShareQtyRow
                {
                    OrderCategory = g.Key.OrderCategory,
                    InvestorType = g.Key.InvestorType,
                    OrderType = g.Key.OrderType,
                    TotalQty = g.Sum(x => x.Quantity)
                })
                .ToListAsync();

            return new ShareQtyData { Rows = rows };
        }

        /// <summary>
        /// Get share qty from child.AllotedQty (actual allotment-based)
        /// </summary>
        public async Task<ShareQtyData> GetAllotedShareQtyDataAsync(int ipoId, int companyId)
        {
            var rows = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder).ThenInclude(o => o.BuyerMaster)
                .Where(c =>
                    c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                    c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                    c.IPOOrder.BuyerMaster.IsActive &&
                    !c.IPOOrder.BuyerMaster.IsDeleted &&
                    !c.IPOOrder.IsDeleted &&
                    !c.IsDeleted)
                .GroupBy(c => new
                {
                    c.IPOOrder.OrderCategory,
                    c.IPOOrder.InvestorType,
                    c.IPOOrder.OrderType
                })
                .Select(g => new ShareQtyRow
                {
                    OrderCategory = g.Key.OrderCategory,
                    InvestorType = g.Key.InvestorType,
                    OrderType = g.Key.OrderType,
                    TotalQty = g.Sum(x => x.AllotedQty ?? 0)
                })
                .ToListAsync();

            return new ShareQtyData { Rows = rows };
        }

        public async Task<IPO_Analysis?> GetAnalysisAsync(int ipoId, int analysisType, int companyId)
        {
            return await _context.Set<IPO_Analysis>()
                .FirstOrDefaultAsync(a =>
                    a.IPOId == ipoId &&
                    a.AnalysisType == analysisType &&
                    a.CompanyId == companyId &&
                    a.IsActive);
        }

        public async Task<List<IPO_Analysis>> GetAllAnalysesAsync(int ipoId, int companyId)
        {
            return await _context.Set<IPO_Analysis>()
                .Where(a => a.IPOId == ipoId && a.CompanyId == companyId && a.IsActive)
                .OrderBy(a => a.AnalysisType)
                .ToListAsync();
        }

        public async Task<ActualAllottedQtySummary> GetActualAllottedQtySummaryAsync(int ipoId, int companyId)
        {
            var summary = new ActualAllottedQtySummary();

            var grouped = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder).ThenInclude(o => o.BuyerMaster)
                .Where(c =>
                    c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                    c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                    c.IPOOrder.BuyerMaster.IsActive &&
                    !c.IPOOrder.BuyerMaster.IsDeleted &&
                    !c.IPOOrder.IsDeleted &&
                    !c.IsDeleted &&
                    (c.IPOOrder.OrderCategory == (int)IPOOrderCategory.Kostak ||
                     c.IPOOrder.OrderCategory == (int)IPOOrderCategory.SubjectTo))
                .GroupBy(c => c.IPOOrder.InvestorType)
                .Select(g => new
                {
                    InvestorType = g.Key,
                    TotalAllotted = g.Sum(x => x.AllotedQty ?? 0)
                })
                .ToListAsync();

            foreach (var row in grouped)
            {
                switch ((IPOInvestorType)row.InvestorType)
                {
                    case IPOInvestorType.Retail:
                        summary.Retail = row.TotalAllotted;
                        break;
                    case IPOInvestorType.SHNI:
                        summary.SHNI = row.TotalAllotted;
                        break;
                    case IPOInvestorType.BHNI:
                        summary.BHNI = row.TotalAllotted;
                        break;
                }
            }

            summary.Total = summary.Retail + summary.SHNI + summary.BHNI;
            return summary;
        }

        public async Task<int> UpsertAnalysisAsync(IPO_Analysis analysis)
        {
            var existing = await _context.Set<IPO_Analysis>()
                .FirstOrDefaultAsync(a =>
                    a.IPOId == analysis.IPOId &&
                    a.AnalysisType == analysis.AnalysisType &&
                    a.CompanyId == analysis.CompanyId &&
                    a.IsActive);

            if (existing != null)
            {
                // Only update fields that are not null — preserve existing DB values for fields not sent
                if (analysis.ExpectedApplications_Retail.HasValue)
                    existing.ExpectedApplications_Retail = analysis.ExpectedApplications_Retail;
                if (analysis.ExpectedApplications_SHNI.HasValue)
                    existing.ExpectedApplications_SHNI = analysis.ExpectedApplications_SHNI;
                if (analysis.ExpectedApplications_BHNI.HasValue)
                    existing.ExpectedApplications_BHNI = analysis.ExpectedApplications_BHNI;
                if (analysis.ActualAllottedQty_Total.HasValue)
                    existing.ActualAllottedQty_Total = analysis.ActualAllottedQty_Total;
                if (analysis.ActualAllottedQty_Retail.HasValue)
                    existing.ActualAllottedQty_Retail = analysis.ActualAllottedQty_Retail;
                if (analysis.ActualAllottedQty_SHNI.HasValue)
                    existing.ActualAllottedQty_SHNI = analysis.ActualAllottedQty_SHNI;
                if (analysis.ActualAllottedQty_BHNI.HasValue)
                    existing.ActualAllottedQty_BHNI = analysis.ActualAllottedQty_BHNI;
                if (analysis.ProfitMargin.HasValue)
                    existing.ProfitMargin = analysis.ProfitMargin;
                if (analysis.SpotPremium.HasValue)
                    existing.SpotPremium = analysis.SpotPremium;
                if (analysis.SpotPrice.HasValue)
                    existing.SpotPrice = analysis.SpotPrice;
                existing.CalculatedResultJson = analysis.CalculatedResultJson;
                existing.ModifiedBy = analysis.ModifiedBy ?? analysis.CreatedBy;
                existing.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existing.Id;
            }
            else
            {
                await _context.Set<IPO_Analysis>().AddAsync(analysis);
                await _context.SaveChangesAsync();
                return analysis.Id;
            }
        }

        public async Task<SharedAnalysisFields> GetLatestSharedFieldsAsync(int ipoId, int companyId)
        {
            // Get the most recently saved analysis for this IPO to read shared fields
            var latest = await _context.Set<IPO_Analysis>()
                .Where(a => a.IPOId == ipoId && a.CompanyId == companyId && a.IsActive)
                .OrderByDescending(a => a.ModifiedDate ?? a.CreatedDate)
                .FirstOrDefaultAsync();

            return new SharedAnalysisFields
            {
                ProfitMargin = latest?.ProfitMargin,
                SpotPremium = latest?.SpotPremium,
                SpotPrice = latest?.SpotPrice
            };
        }

        public async Task UpdateSharedFieldsAsync(int ipoId, int companyId, decimal? profitMargin, decimal? spotPremium, decimal? spotPrice)
        {
            // Update shared fields across ALL analysis tabs for this IPO
            var allAnalyses = await _context.Set<IPO_Analysis>()
                .Where(a => a.IPOId == ipoId && a.CompanyId == companyId && a.IsActive)
                .ToListAsync();

            foreach (var a in allAnalyses)
            {
                // Only update shared fields that were explicitly provided (not null)
                if (profitMargin.HasValue)
                    a.ProfitMargin = profitMargin;
                if (spotPremium.HasValue)
                    a.SpotPremium = spotPremium;
                if (spotPrice.HasValue)
                    a.SpotPrice = spotPrice;
                a.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
