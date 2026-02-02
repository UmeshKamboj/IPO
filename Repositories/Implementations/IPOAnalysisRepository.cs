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

        public async Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(int ipoId, int companyId)
        {
            var response = new OrderStatusSummaryResponse();

            var grouped = await _context.BuyerOrders
                .Include(o => o.BuyerMaster)
                .Where(o =>
                    o.BuyerMaster.CompanyId == companyId &&
                    o.BuyerMaster.IPOId == ipoId &&
                    o.BuyerMaster.IsActive && !o.BuyerMaster.IsDeleted && !o.IsDeleted)
                .GroupBy(o => new { o.OrderCategory, o.InvestorType, o.OrderType })
                .Select(g => new
                {
                    g.Key.OrderCategory,
                    g.Key.InvestorType,
                    g.Key.OrderType,
                    Count = g.Sum(x => x.Quantity),
                    Avg = g.Average(x => x.Rate),
                    Amount = g.Sum(x => x.Quantity * x.Rate)
                })
                .ToListAsync();

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
                    item.Net.Amount = item.Buy.Amount - item.Sell.Amount;
                    item.Net.Avg = item.Net.Count == 0 ? 0 : item.Net.Amount / item.Net.Count;
                }
            }
            CalcNet(response.Kostak);
            CalcNet(response.SubjectTo);

            // Premium
            foreach (var row in grouped.Where(x => x.OrderCategory == (int)IPOOrderCategory.Premium))
            {
                var target = row.OrderType == (int)IPOOrderType.BUY
                    ? response.Premium.Buy : response.Premium.Sell;
                target.Count += row.Count;
                target.Amount += row.Amount;
                target.Avg = target.Count == 0 ? 0 : target.Amount / target.Count;
            }
            response.Premium.Net.Count = response.Premium.Buy.Count - response.Premium.Sell.Count;
            response.Premium.Net.Amount = response.Premium.Buy.Amount - response.Premium.Sell.Amount;
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
                existing.ExpectedApplications_Retail = analysis.ExpectedApplications_Retail;
                existing.ExpectedApplications_SHNI = analysis.ExpectedApplications_SHNI;
                existing.ExpectedApplications_BHNI = analysis.ExpectedApplications_BHNI;
                existing.ActualAllottedQty_Total = analysis.ActualAllottedQty_Total;
                existing.ActualAllottedQty_Retail = analysis.ActualAllottedQty_Retail;
                existing.ActualAllottedQty_SHNI = analysis.ActualAllottedQty_SHNI;
                existing.ActualAllottedQty_BHNI = analysis.ActualAllottedQty_BHNI;
                existing.ProfitMargin = analysis.ProfitMargin;
                existing.SpotPremium = analysis.SpotPremium;
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
    }
}
