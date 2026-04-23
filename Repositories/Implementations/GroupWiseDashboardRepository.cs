using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.GroupWiseDashboard;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class GroupWiseDashboardRepository:BaseRepository<IPO_PaymentTransaction>, IGroupWiseDashboardRepository
    {
        public GroupWiseDashboardRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<GroupIpoSummaryRow>> GetGroupWiseDashboardSummaryAsync(GroupWiseSummaryRequest request, int companyId)
        {

            // PAGED GROUPS (MASTER)
            var groupQuery = _context.IPO_GroupMaster
                .Where(g => g.CompanyId == companyId && g.IsActive);

            // Apply text search on group name if provided
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue.Trim();
                groupQuery = groupQuery.Where(g => g.GroupName.Contains(search));
            }

            var totalGroups = await groupQuery.CountAsync();

            var pagedGroups = await groupQuery
                .OrderBy(g => g.GroupName)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .Select(g => new { g.IPOGroupId, g.GroupName })
                .ToListAsync();

            var groupIds = pagedGroups.Select(x => x.IPOGroupId).ToList();

            //  ALL IPOS (MASTER)
            var ipos = await _context.IPO_IPOMaster
                .Where(i => i.CompanyId == companyId && i.IsActive)
                .Select(i => new { i.Id, i.IPOName })
                .ToListAsync();

            // TRANSACTION SUMMARY (ONLY EXISTING)
            var summary = await _context.PaymentTransactions
                .Where(x => x.CompanyId == companyId
                         && groupIds.Contains(x.GroupId))
                .GroupBy(x => new { x.GroupId, x.IpoId })
                .Select(g => new
                {
                    g.Key.GroupId,
                    g.Key.IpoId,
                    Debit = g.Where(x => x.AmountType == (int)AmountType.Debit)
                             .Sum(x => x.Amount),
                    Credit = g.Where(x => x.AmountType == (int)AmountType.Credit)
                              .Sum(x => x.Amount)
                })
                .ToListAsync();

            // JV TRANSACTION SUMMARY (IsJVTransaction = true)
            var jvSummary = await _context.PaymentTransactions
                .Where(x => x.CompanyId == companyId
                         && groupIds.Contains(x.GroupId)
                         && x.IsJVTransaction)
                .GroupBy(x => new { x.GroupId, x.IpoId })
                .Select(g => new
                {
                    g.Key.GroupId,
                    g.Key.IpoId,
                    JV = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            //  MERGE (ZERO FILL)
            var result = new List<GroupIpoSummaryRow>();

            foreach (var g in pagedGroups)
            {
                foreach (var ipo in ipos)
                {
                    var match = summary.FirstOrDefault(x =>
                        x.GroupId == g.IPOGroupId && x.IpoId == ipo.Id);
                    var jvMatch = jvSummary.FirstOrDefault(x =>
                        x.GroupId == g.IPOGroupId && x.IpoId == ipo.Id);

                    result.Add(new GroupIpoSummaryRow
                    {
                        GroupId = g.IPOGroupId,
                        GroupName = g.GroupName,
                        IpoId = ipo.Id,
                        IpoName = ipo.IPOName,
                        Debit = match?.Debit ?? 0,
                        Credit = match?.Credit ?? 0,
                        JV = jvMatch?.JV ?? 0
                    });
                }
            }

            return new PagedResult<GroupIpoSummaryRow>(result, totalGroups, request.Skip, request.PageSize);

        }

        public async Task<List<GroupIpoSummaryRow>> GetAllGroupWiseDashboardSummaryAsync(int companyId)
        {
            // ALL GROUPS
            var allGroups = await _context.IPO_GroupMaster
                .Where(g => g.CompanyId == companyId && g.IsActive)
                .OrderBy(g => g.GroupName)
                .Select(g => new { g.IPOGroupId, g.GroupName })
                .ToListAsync();

            var groupIds = allGroups.Select(x => x.IPOGroupId).ToList();

            // ALL IPOS
            var ipos = await _context.IPO_IPOMaster
                .Where(i => i.CompanyId == companyId && i.IsActive)
                .Select(i => new { i.Id, i.IPOName })
                .ToListAsync();

            // TRANSACTION SUMMARY
            var summary = await _context.PaymentTransactions
                .Where(x => x.CompanyId == companyId
                         && groupIds.Contains(x.GroupId))
                .GroupBy(x => new { x.GroupId, x.IpoId })
                .Select(g => new
                {
                    g.Key.GroupId,
                    g.Key.IpoId,
                    Debit = g.Where(x => x.AmountType == (int)AmountType.Debit)
                             .Sum(x => x.Amount),
                    Credit = g.Where(x => x.AmountType == (int)AmountType.Credit)
                              .Sum(x => x.Amount)
                })
                .ToListAsync();

            // JV TRANSACTION SUMMARY
            var jvSummary = await _context.PaymentTransactions
                .Where(x => x.CompanyId == companyId
                         && groupIds.Contains(x.GroupId)
                         && x.IsJVTransaction)
                .GroupBy(x => new { x.GroupId, x.IpoId })
                .Select(g => new
                {
                    g.Key.GroupId,
                    g.Key.IpoId,
                    JV = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            // MERGE (ZERO FILL)
            var result = new List<GroupIpoSummaryRow>();

            foreach (var g in allGroups)
            {
                foreach (var ipo in ipos)
                {
                    var match = summary.FirstOrDefault(x =>
                        x.GroupId == g.IPOGroupId && x.IpoId == ipo.Id);
                    var jvMatch = jvSummary.FirstOrDefault(x =>
                        x.GroupId == g.IPOGroupId && x.IpoId == ipo.Id);

                    result.Add(new GroupIpoSummaryRow
                    {
                        GroupId = g.IPOGroupId,
                        GroupName = g.GroupName,
                        IpoId = ipo.Id,
                        IpoName = ipo.IPOName,
                        Debit = match?.Debit ?? 0,
                        Credit = match?.Credit ?? 0,
                        JV = jvMatch?.JV ?? 0
                    });
                }
            }

            return result;
        }

        public async Task<List<GroupIpoBillingRow>> GetOrderBillingByGroupAsync(List<int> groupIds, int companyId)
        {
            // Fetch all child orders for the given groups
            var children = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder).ThenInclude(o => o.BuyerMaster)
                .Where(c =>
                    c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                    groupIds.Contains(c.GroupId) &&
                    c.IPOOrder.BuyerMaster.IsActive &&
                    !c.IPOOrder.BuyerMaster.IsDeleted &&
                    !c.IPOOrder.IsDeleted &&
                    !c.IsDeleted)
                .ToListAsync();

            // Fetch IPO master data for pricing
            var ipoIds = children.Select(c => c.IPOOrder.BuyerMaster.IPOId).Distinct().ToList();
            var ipoMasters = await _context.IPO_IPOMaster
                .Where(i => ipoIds.Contains(i.Id) && i.CompanyId == companyId)
                .ToDictionaryAsync(i => i.Id);

            // Calculate billing using correct formula per OrderCategory
            var rows = children
                .GroupBy(c => new { c.GroupId, c.IPOOrder.BuyerMaster.IPOId })
                .Select(g =>
                {
                    var ipoMaster = ipoMasters.GetValueOrDefault(g.Key.IPOId);
                    var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
                    var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

                    var billingTotal = g.Sum(c =>
                    {
                        var order = c.IPOOrder;
                        var allotedQty = c.AllotedQty ?? 0;
                        var preOpenPrice = c.PreOpenPrice > 0 ? c.PreOpenPrice : ipoPreOpenPrice;
                        var orderCategory = order.OrderCategory;
                        bool isPremOpt = orderCategory == (int)IPOOrderCategory.Premium ||
                                         orderCategory == (int)IPOOrderCategory.CALL ||
                                         orderCategory == (int)IPOOrderCategory.PUT;
                        var rate = isPremOpt && order.EffectiveRate.HasValue
                            ? order.EffectiveRate.Value
                            : order.Rate;

                        decimal amount;
                        if (orderCategory == (int)IPOOrderCategory.CALL)
                        {
                            amount = -rate * allotedQty;
                        }
                        else if (orderCategory == (int)IPOOrderCategory.PUT)
                        {
                            decimal putSp = 0;
                            if (!string.IsNullOrEmpty(c.IPOOrder.PremiumStrikePrice)
                                && decimal.TryParse(c.IPOOrder.PremiumStrikePrice, out var parsedSp))
                                putSp = parsedSp;
                            amount = (ipoPrice - preOpenPrice - rate + putSp) * allotedQty;
                        }
                        else if (isPremOpt)
                        {
                            amount = (preOpenPrice - ipoPrice - rate) * allotedQty;
                        }
                        else
                        {
                            amount = allotedQty == 0 ? 0 : (preOpenPrice - ipoPrice) * allotedQty - rate;
                        }

                        // StrikePrice already added to Rate for CALL/PUT

                        if (order.OrderType == (int)IPOOrderType.SELL)
                            amount = -amount;

                        return amount;
                    });

                    return new GroupIpoBillingRow
                    {
                        GroupId = g.Key.GroupId,
                        IpoId = g.Key.IPOId,
                        BillingTotal = billingTotal
                    };
                })
                .ToList();

            return rows;
        }
    }
}
