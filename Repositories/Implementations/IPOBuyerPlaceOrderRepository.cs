using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPOClient.Repositories.Implementations
{
    public class IPOBuyerPlaceOrderRepository: BaseRepository<IPO_BuyerPlaceOrderMaster>,IIPOBuyerPlaceOrderRepository
    {
        public IPOBuyerPlaceOrderRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<int> CreateAsync(IPOBuyerPlaceOrderRequest request, int userId, int companyId)
        {
            // Get IPO master for PreOpenPrice and lot sizes
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(i => i.Id == request.IPOId && i.CompanyId == companyId);
            var preOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

            var master = new IPO_BuyerPlaceOrderMaster
            {
                IPOId = request.IPOId,
                CreatedBy = userId,
                CompanyId = companyId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                Orders = new List<IPO_BuyerOrder>()
            };

            foreach (var reqOrder in request.Orders)
            {
                // Determine PremiumStrikePrice based on ApplicateRate
                // Skip logic if OrderCategory is CALL (4) or PUT (5)
                string? premiumStrikePrice = reqOrder.PremiumStrikePrice;
                if (reqOrder.OrderCategory != 4 && reqOrder.OrderCategory != 5)
                {
                    premiumStrikePrice = reqOrder.ApplicateRate ? "Premium" : "Application";
                }

                // User enters the rate as-is. Store it directly.
                // For Premium/CALL/PUT: also store as EffectiveRate so display always shows what user entered.
                bool isPremOrOpt = reqOrder.OrderCategory == (int)IPOOrderCategory.Premium ||
                                   reqOrder.OrderCategory == (int)IPOOrderCategory.CALL ||
                                   reqOrder.OrderCategory == (int)IPOOrderCategory.PUT;

                var enteredRate = reqOrder.Rate;
                var storedRate = enteredRate;
                decimal? calcEffectiveRate = isPremOrOpt ? enteredRate : null;

                var order = new IPO_BuyerOrder
                {
                    OrderType = reqOrder.OrderType,
                    OrderCategory = reqOrder.OrderCategory,
                    InvestorType = reqOrder.InvestorType,
                    PremiumStrikePrice = premiumStrikePrice,
                    ApplicateRate = reqOrder.ApplicateRate,
                    Quantity = reqOrder.Quantity,
                    Rate = storedRate,
                    DateTime = request.DateTime,
                    CreatedBy = userId,
                    CompanyId = companyId,
                    OrderCreatedDate= DateTime.UtcNow,
                    OrderChild = new List<IPO_PlaceOrderChild>(),
                    Remarks= request.RemarksIds,
                    OrderSource = (int)OrderSourceType.Manual,
                    EffectiveRate = calcEffectiveRate
                };

                // AllotedQty: Premium/CALL/PUT = 1 per child (rate is per-share)
                // Kostak/SubjectTo (Retail/SHNI/BHNI) = null (set later by allotment check)
                int? allotedQty = (reqOrder.OrderCategory == (int)IPOOrderCategory.Premium ||
                                   reqOrder.OrderCategory == (int)IPOOrderCategory.CALL ||
                                   reqOrder.OrderCategory == (int)IPOOrderCategory.PUT)
                    ? 1 : null;

                // split
                for (int i = 0; i < reqOrder.Quantity; i++)
                {
                    order.OrderChild.Add(new IPO_PlaceOrderChild
                    {
                        Quantity = 1,
                        GroupId = request.GroupId, // Set GroupId from master
                        PreOpenPrice = preOpenPrice, // Set from IPO parent
                        AllotedQty = allotedQty, // Set lot size based on investor type
                        CreatedBy = userId,
                        CompanyId = companyId,
                        ChildOrderCreatedDate= DateTime.UtcNow,
                    });
                }

                master.Orders.Add(order);
            }

            await _dbSet.AddAsync(master);
            await _context.SaveChangesAsync();

            return master.BuyerMasterId;

        }
        public async Task<IPO_BuyerPlaceOrderMaster?> GetByIdAsync(int id, int companyId)
        {
            return await _context.BuyerPlaceOrderMasters
                .Include(m => m.Orders)
                .FirstOrDefaultAsync(m => m.BuyerMasterId == id && m.IsActive && m.CompanyId == companyId && !m.IsDeleted);
        }

       

        public async Task<List<IPO_BuyerOrder>> GetTopFivePlaceOrderListAsync(int ipoId, int companyId)
        {
            var orders = await _context.BuyerOrders
              .Include(o => o.BuyerMaster)
              .Include(o => o.OrderChild)
                  .ThenInclude(c => c.Group)
              .Where(o => o.BuyerMaster.IPOId == ipoId
                && o.BuyerMaster.CompanyId == companyId
                && o.BuyerMaster.IsActive && !o.BuyerMaster.IsDeleted && !o.IsDeleted)
               .OrderByDescending(o => o.OrderId)
               .Take(5)
               .ToListAsync();
            foreach (var order in orders)
            {
                if (!string.IsNullOrEmpty(order.Remarks))
                {
                    var remarkIds = order.Remarks.Split(',')
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
                    if (remarkIds.Any())
                    {
                        var remarkNames = await _context.IPO_OrderRemark
                       .Where(r => remarkIds.Contains(r.RemarkId) && r.CompanyId == companyId && r.IsActive)
                       .Select(r => r.Remark)
                       .ToListAsync();

                        order.Remarks = string.Join(", ", remarkNames);
                    }

                }
                else
                {
                    order.Remarks = "-";
                }
            }
            return orders;
        }
        public async Task<PagedResult<IPO_BuyerOrder>> GetOrderPagedListAsync(OrderDetailPagedRequest request, int companyId,int ipoId)
        {
            // Base query
            var query = _context.BuyerOrders
                .Include(o => o.BuyerMaster) // Parent included
                .Include(o => o.OrderChild)
                    .ThenInclude(c => c.Group)
                .AsQueryable();
            // Only active and belong to company
            query = query.Where(o =>
                o.BuyerMaster.IsActive &&
                o.BuyerMaster.CompanyId == companyId &&
                o.BuyerMaster.IPOId == ipoId && !o.BuyerMaster.IsDeleted && !o.IsDeleted);

            // Apply global search if provided
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue?.Trim().ToLower();
                int? orderTypeMatch = Enum.GetValues(typeof(IPOOrderType)).Cast<IPOOrderType>()
                .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                int? orderCategoryMatch = Enum.GetValues(typeof(IPOOrderCategory)).Cast<IPOOrderCategory>()
                    .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                int? investorTypeMatch = Enum.GetValues(typeof(IPOInvestorType)).Cast<IPOInvestorType>()
                    .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                query = query.Where(o =>
                 // search
                   (orderTypeMatch.HasValue && o.OrderType == orderTypeMatch.Value)
                 || (orderCategoryMatch.HasValue && o.OrderCategory == orderCategoryMatch.Value)
                 || (investorTypeMatch.HasValue && o.InvestorType == investorTypeMatch.Value)
                 || ( o.PremiumStrikePrice == request.SearchValue)
                 // 🔹 Group name - search through child's Group
                 || (o.OrderChild.Any(c => c.Group != null && c.Group.GroupName.Contains(search)))
             );
            }
            // Filter by GroupId through child table
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(o => o.OrderChild.Any(c => c.GroupId == request.GroupId.Value));
            if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
                query = query.Where(o => o.OrderCategory == request.OrderCategoryId.Value);
            if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
                query = query.Where(o => o.InvestorType == request.InvestorTypeId.Value);
            query = query.OrderByDescending(o => o.BuyerMaster.CreatedDate);

            // Total count before pagination
            var totalCount = await query.CountAsync();
            var totalApplications = await query.CountAsync();
            var pendingPanApplications = await query.CountAsync(o =>o.OrderChild.Any(c => string.IsNullOrEmpty(c.PANNumber)));
            // Apply offset-based pagination
            var list = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();
              var pagedResult = new PagedResult<IPO_BuyerOrder>(
               list,
               totalCount,
               request.Skip,
               request.PageSize
              );
             pagedResult.Extras = new Dictionary<string, int>
             {
                 { "totalApplications", totalApplications },
                 { "pendingPanApplications", pendingPanApplications }
             };
            return pagedResult;

        }

        public async Task<List<IPO_BuyerOrder>> GetOrderListAsync(OrderListRequest request, int companyId, int ipoId)
        {
            // Base query
            var query = _context.BuyerOrders
                .Include(o => o.BuyerMaster)
                .Include(o => o.OrderChild)
                    .ThenInclude(c => c.Group)
                .AsQueryable();

            // Only active and belong to company
            query = query.Where(o =>
                o.BuyerMaster.IsActive &&
                o.BuyerMaster.CompanyId == companyId &&
                o.BuyerMaster.IPOId == ipoId && !o.BuyerMaster.IsDeleted && !o.IsDeleted);

            // Apply group filter through child table (no pagination, no global search)
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(o => o.OrderChild.Any(c => c.GroupId == request.GroupId.Value));

            query = query.OrderByDescending(o => o.BuyerMaster.CreatedDate);

            // Return all results without pagination
            return await query.ToListAsync();
        }

        public async Task<PagedResult<IPO_PlaceOrderChild>> GetOrderDetailPagedListAsync(OrderDetailPagedRequest request, int companyId, int ipoId, int orderType)
        {
            var query = _context.ChildPlaceOrder
         .Include(c => c.IPOOrder)
             .ThenInclude(o => o.BuyerMaster)
         .Include(c => c.Group)
         .AsQueryable();

            query = query.Where(c =>
                c.IPOOrder.BuyerMaster.IsActive &&
                c.IPOOrder.BuyerMaster.CompanyId == companyId && c.IPOOrder.OrderType== orderType &&
                c.IPOOrder.BuyerMaster.IPOId == ipoId &&!c.IsDeleted &&!c.IPOOrder.BuyerMaster.IsDeleted && !c.IPOOrder.IsDeleted && new[] { 1, 2 }.Contains(c.IPOOrder.OrderCategory) && new[] { 1, 2, 3 }.Contains(c.IPOOrder.InvestorType)
            );
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                query = query.Where(c =>
                    (c.PANNumber != null && c.PANNumber.Contains(request.SearchValue)) ||
                    (c.ClientName != null && c.ClientName.Contains(request.SearchValue)) ||
                    (c.DematNumber != null && c.DematNumber.Contains(request.SearchValue)) ||
                    (c.ApplicationNo != null && c.ApplicationNo.Contains(request.SearchValue))
                );
            }

            // Filters - GroupId filter on child table directly
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.GroupId == request.GroupId.Value);

            if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
                query = query.Where(c => c.IPOOrder.OrderCategory == request.OrderCategoryId.Value);

            if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
                query = query.Where(c => c.IPOOrder.InvestorType == request.InvestorTypeId.Value);

            query = query.OrderByDescending(c => c.IPOOrder.BuyerMaster.CreatedDate);

            var totalCount = await query.CountAsync();
            var pendingPanApplications = await query.CountAsync(c => string.IsNullOrEmpty(c.PANNumber));

            var list = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            var result = new PagedResult<IPO_PlaceOrderChild>(
                list,
                totalCount,
                request.Skip,
                request.PageSize
            );

            result.Extras = new Dictionary<string, int>
            {
                { "totalApplications", totalCount },
                { "pendingPanApplications", pendingPanApplications }
            };

            return result;
        }

        public async Task<PagedResult<IPO_PlaceOrderChild>> GetOrderDetailPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType)
        {
            var query = _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Include(c => c.Group)
                .AsQueryable();

            query = query.Where(c =>
                c.IPOOrder.BuyerMaster.IsActive &&
                c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                c.IPOOrder.OrderType == orderType &&
                c.IPOOrder.BuyerMaster.IPOId == ipoId && !c.IsDeleted && !c.IPOOrder.BuyerMaster.IsDeleted && !c.IPOOrder.IsDeleted &&
                new[] { 1, 2 }.Contains(c.IPOOrder.OrderCategory) &&
                new[] { 1, 2, 3 }.Contains(c.IPOOrder.InvestorType)
            );

            // Apply global search filter
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                query = query.Where(c =>
                    (c.PANNumber != null && c.PANNumber.Contains(request.SearchValue)) ||
                    (c.ClientName != null && c.ClientName.Contains(request.SearchValue)) ||
                    (c.DematNumber != null && c.DematNumber.Contains(request.SearchValue)) ||
                    (c.ApplicationNo != null && c.ApplicationNo.Contains(request.SearchValue))
                );
            }

            // Apply group filter on child table directly
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.GroupId == request.GroupId.Value);

            // Apply category and investor type filters
            if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
                query = query.Where(c => c.IPOOrder.OrderCategory == request.OrderCategoryId.Value);

            if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
                query = query.Where(c => c.IPOOrder.InvestorType == request.InvestorTypeId.Value);

            // Get total count before pagination
            var totalCount = await query.CountAsync();
            var pendingPanApplications = await query.CountAsync(c => string.IsNullOrEmpty(c.PANNumber));
            // Order and apply pagination
            query = query.OrderByDescending(c => c.IPOOrder.BuyerMaster.CreatedDate)
                         .Skip(request.Skip)
                         .Take(request.PageSize);

            var items = await query.ToListAsync();
            foreach (var order in items)
            {
                string remark = order.IPOOrder.Remarks??"";
                if (!string.IsNullOrEmpty(remark))
                {
                    var remarkIds = remark.Split(',')
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
                    if (remarkIds.Any())
                    {
                        var remarkNames = await _context.IPO_OrderRemark
                      .Where(r => remarkIds.Contains(r.RemarkId) && r.CompanyId == companyId && r.IsActive)
                      .Select(r => r.Remark)
                      .ToListAsync();

                        order.IPOOrder.Remarks = string.Join(", ", remarkNames);
                    }
                    
                  
                }
                else
                {
                    order.IPOOrder.Remarks = "-";
                }
            }
            var result = new PagedResult<IPO_PlaceOrderChild>(
                 items,
                 totalCount,
                 request.Skip,
                 request.PageSize
             );

            result.Extras = new Dictionary<string, int>
            {
                { "totalApplications", totalCount },
                { "pendingPanApplications", pendingPanApplications }
            };
            return result;
        }

        public async Task<(int code, string message)> UpdateOrderDetailsAsync(UpdateOrderDetailsListRequest request, int userId)
        {
            if (request == null || request.Orders == null || !request.Orders.Any())
                return (-1, "Order details not found or inactive");

            var poChildIds = request.Orders.Select(x => x.POChildId).ToList();

            // Find all OrderIds for those POChildIds
            var orderIds = await _context.ChildPlaceOrder
                .Where(x => poChildIds.Contains(x.POChildId))
                .Select(x => x.OrderId)
                .Distinct()
                .ToListAsync();




            //  Request level PAN duplicate check
            var duplicatePanInRequest = request.Orders
                .Where(x => !string.IsNullOrWhiteSpace(x.PANNumber))
                .GroupBy(x => x.PANNumber.Trim().ToUpper())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatePanInRequest.Any())
            {
                return (-1, $"Duplicate PAN in request: {string.Join(", ", duplicatePanInRequest)}");
                
            }

            // Db level PAN duplicate check
             var panList = request.Orders
            .Where(x => !string.IsNullOrWhiteSpace(x.PANNumber))
            .Select(x => x.PANNumber.Trim())
            .Distinct()
            .ToList();
            var duplicatePan = await _context.ChildPlaceOrder
           .Where(x => panList.Contains(x.PANNumber)
             && !poChildIds.Contains(x.POChildId) && orderIds.Contains(x.OrderId)) // same record ko ignore
           .Select(x => x.PANNumber)
           .FirstOrDefaultAsync();

            if (duplicatePan != null)
            {
                return (-1, $"PAN already exists: {duplicatePan}");

            }


            var POChildOrder = await _context.ChildPlaceOrder
                .Where(c => poChildIds.Contains(c.POChildId))
                .ToListAsync();
            foreach (var order in POChildOrder)
            {
                var req = request.Orders.First(x => x.POChildId == order.POChildId);

                if (!string.IsNullOrWhiteSpace(req.PANNumber))
                    order.PANNumber = req.PANNumber;

                if (!string.IsNullOrWhiteSpace(req.ClientName))
                    order.ClientName = req.ClientName;

                if (!string.IsNullOrWhiteSpace(req.DematNumber))
                    order.DematNumber = req.DematNumber;

                if (!string.IsNullOrWhiteSpace(req.ApplicationNumber))
                    order.ApplicationNo = req.ApplicationNumber;

                if (req.AllotedQty.HasValue)
                    order.AllotedQty = req.AllotedQty;

                order.ModifiedBy = userId.ToString();
                order.ModifiedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return (1, "Order details updated successfully");
        }
        public async Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(OrderStatusFilterRequest request, int companyId)
        {
            var response = new OrderStatusSummaryResponse();

            // Get IPO Master data for pricing
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(i => i.Id == request.IPOId && i.CompanyId == companyId);
            var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
            var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

            // Get child orders with all required data
            var childQuery = _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Where(c =>
                    c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                    c.IPOOrder.BuyerMaster.IPOId == request.IPOId &&
                    c.IPOOrder.BuyerMaster.IsActive && !c.IPOOrder.BuyerMaster.IsDeleted && !c.IPOOrder.IsDeleted && !c.IsDeleted)
                .AsQueryable();

            // Filter by GroupId through child table
            if (request.GroupId.HasValue && request.GroupId > 0)
                childQuery = childQuery.Where(c => c.GroupId == request.GroupId);

            if (request.InvestorType.HasValue && request.InvestorType > 0)
                childQuery = childQuery.Where(c => c.IPOOrder.InvestorType == request.InvestorType);
            if (request.OrderCategory.HasValue && request.OrderCategory > 0)
                childQuery = childQuery.Where(c => c.IPOOrder.OrderCategory == request.OrderCategory);

            var childOrders = await childQuery.ToListAsync();

            // =========================
            // GROUPED DATA with new formula
            // =========================
            var grouped = childOrders
                .GroupBy(c => new
                {
                    c.IPOOrder.OrderCategory,
                    c.IPOOrder.InvestorType,
                    c.IPOOrder.OrderType,
                    c.IPOOrder.PremiumStrikePrice
                })
                .Select(g =>
                {
                    var count = g.Count();
                    var totalAmount = g.Sum(c =>
                    {
                        var allotedQty = c.AllotedQty ?? 0;
                        var preOpenPrice = c.PreOpenPrice > 0 ? c.PreOpenPrice : ipoPreOpenPrice;
                        var orderCategory = c.IPOOrder.OrderCategory;
                        bool isPremOpt = orderCategory == (int)IPOOrderCategory.Premium ||
                                         orderCategory == (int)IPOOrderCategory.CALL ||
                                         orderCategory == (int)IPOOrderCategory.PUT;
                        var rate = isPremOpt && c.IPOOrder.EffectiveRate.HasValue
                            ? c.IPOOrder.EffectiveRate.Value
                            : c.IPOOrder.Rate;

                        decimal amount;
                        if (orderCategory == (int)IPOOrderCategory.CALL)
                        {
                            // CALL: rate-only (Rate × AllotedQty)
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
                        else if (isPremOpt) // Premium
                        {
                            amount = (preOpenPrice - ipoPrice - rate) * allotedQty;
                        }
                        else // Kostak / SubjectTo
                        {
                            amount = allotedQty == 0 ? 0 : (preOpenPrice - ipoPrice) * allotedQty - rate;
                        }

                        // StrikePrice is already added to Rate for CALL

                        if (c.IPOOrder.OrderType == (int)IPOOrderType.SELL)
                            amount = -amount;

                        return amount;
                    });
                    var avgRate = count > 0 ? totalAmount / count : 0;

                    return new
                    {
                        g.Key.OrderCategory,
                        g.Key.InvestorType,
                        g.Key.OrderType,
                        g.Key.PremiumStrikePrice,
                        Count = count,
                        Avg = avgRate,
                        Amount = totalAmount
                    };
                })
                .ToList();
            // =========================
            // PRE-INITIALIZE KOSTAK & SUBJECT TO
            // =========================
            var allowedInvestorTypes = new[]
            {
                IPOInvestorType.Retail,
                IPOInvestorType.SHNI,
                IPOInvestorType.BHNI
            };

            foreach (var type in allowedInvestorTypes)
            {
                var key = type.ToString();
                if (!response.Kostak.ContainsKey(key))
                    response.Kostak[key] = new CategoryStatusBlock();

                if (!response.SubjectTo.ContainsKey(key))
                    response.SubjectTo[key] = new CategoryStatusBlock();
            }

            foreach (var type in allowedInvestorTypes)
            {
                var key = type.ToString();
                if (!response.Kostak.ContainsKey(key))
                    response.Kostak[key] = new CategoryStatusBlock();

                if (!response.SubjectTo.ContainsKey(key))
                    response.SubjectTo[key] = new CategoryStatusBlock();
            }

            // =========================
            // KOSTAK & SUBJECT TO
            // =========================
            foreach (var row in grouped
                .Where(x => x.OrderCategory == (int)IPOOrderCategory.Kostak
                         || x.OrderCategory == (int)IPOOrderCategory.SubjectTo))
            {
                var categoryDict = row.OrderCategory == (int)IPOOrderCategory.Kostak
                    ? response.Kostak
                    : response.SubjectTo;

                var investorKey = ((IPOInvestorType)row.InvestorType).ToString();

                if (!categoryDict.ContainsKey(investorKey))
                    categoryDict[investorKey] = new CategoryStatusBlock();

                var block = categoryDict[investorKey];

                var target = row.OrderType == (int)IPOOrderType.BUY
                    ? block.Buy
                    : block.Sell;

                target.Count += row.Count;
                target.Amount += row.Amount;
                target.Avg = target.Count == 0 ? 0 : target.Amount / target.Count;
            }

            // NET calculation
            void CalculateNet(Dictionary<string, CategoryStatusBlock> dict)
            {
                foreach (var item in dict.Values)
                {
                    item.Net.Count = item.Buy.Count - item.Sell.Count;
                    item.Net.Amount = item.Buy.Amount - item.Sell.Amount;
                    item.Net.Avg = item.Net.Count == 0 ? 0 : item.Net.Amount / item.Net.Count;
                }
            }

            CalculateNet(response.Kostak);
            CalculateNet(response.SubjectTo);

            // =========================
            // PREMIUM
            // =========================
            foreach (var row in grouped
                .Where(x => x.OrderCategory == (int)IPOOrderCategory.Premium))
            {
                var target = row.OrderType == (int)IPOOrderType.BUY
                    ? response.Premium.Buy
                    : response.Premium.Sell;

                target.Count += row.Count;
                target.Amount += row.Amount;
                target.Avg = target.Count == 0 ? 0 : target.Amount / target.Count;
            }

            response.Premium.Net.Count =
                response.Premium.Buy.Count - response.Premium.Sell.Count;

            response.Premium.Net.Amount =
                response.Premium.Buy.Amount - response.Premium.Sell.Amount;

            response.Premium.Net.Avg =
                response.Premium.Net.Count == 0 ? 0 :
                response.Premium.Net.Amount / response.Premium.Net.Count;

            // =========================
            // STRIKE PRICE (CALL / PUT)
            // =========================
            var strikeGroups = grouped
                .Where(x => !string.IsNullOrEmpty(x.PremiumStrikePrice) && x.PremiumStrikePrice != "Application" && x.PremiumStrikePrice != "Premium")
                .GroupBy(x => x.PremiumStrikePrice);

            foreach (var sg in strikeGroups)
            {
                var block = new StrikePriceBlock
                {
                    StrikePrice = decimal.Parse(sg.Key!)
                };

                var call = sg.Where(x => x.OrderType == (int)IPOOrderType.BUY);
                var put = sg.Where(x => x.OrderType == (int)IPOOrderType.SELL);

                block.Call_TotalShare = call.Sum(x => x.Count);
                block.Call_Amount = call.Sum(x => x.Amount);
                block.Call_Avg = block.Call_TotalShare == 0 ? 0 : block.Call_Amount / block.Call_TotalShare;

                block.Put_TotalShare = put.Sum(x => x.Count);
                block.Put_Amount = put.Sum(x => x.Amount);
                block.Put_Avg = block.Put_TotalShare == 0 ? 0 : block.Put_Amount / block.Put_TotalShare;

                response.StrikePrices.Add(block);
            }

            //  StrikePrices never empty
            if (!response.StrikePrices.Any())
            {
                response.StrikePrices.Add(new StrikePriceBlock
                {
                    StrikePrice = 0,
                    Call_TotalShare = 0,
                    Call_Avg = 0,
                    Call_Amount = 0,
                    Put_TotalShare = 0,
                    Put_Avg = 0,
                    Put_Amount = 0
                });
            }
            return response;
        }
        //public async Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(OrderStatusFilterRequest request, int companyId)
        //{
        //    var response = new OrderStatusSummaryResponse();

        //    var query = _context.BuyerOrders
        //        .Include(o => o.BuyerMaster)
        //        .Include(o => o.OrderChild)
        //        .Where(o =>
        //            o.BuyerMaster.CompanyId == companyId &&
        //            o.BuyerMaster.IPOId == request.IPOId &&
        //            o.BuyerMaster.IsActive&&!o.BuyerMaster.IsDeleted&&!o.IsDeleted)
        //        .AsQueryable();

        //    // Filter by GroupId through child table
        //    if (request.GroupId.HasValue&& request.GroupId>0)
        //        query = query.Where(o => o.OrderChild.Any(c => c.GroupId == request.GroupId));

        //    if (request.InvestorType.HasValue&& request.InvestorType>0)
        //        query = query.Where(o => o.InvestorType == request.InvestorType);
        //    if (request.OrderCategory.HasValue && request.OrderCategory > 0)
        //        query = query.Where(o => o.OrderCategory == request.OrderCategory);

        //    // =========================
        //    // GROUPED DATA
        //    // =========================
        //    var grouped = await query
        //        .GroupBy(o => new
        //        {
        //            o.OrderCategory,
        //            o.InvestorType,
        //            o.OrderType,
        //            o.PremiumStrikePrice
        //        })
        //        .Select(g => new
        //        {
        //            g.Key.OrderCategory,
        //            g.Key.InvestorType,
        //            g.Key.OrderType,
        //            g.Key.PremiumStrikePrice,
        //            Count = g.Sum(x => x.Quantity),
        //            Avg = g.Average(x => x.Rate),
        //            Amount = g.Sum(x => x.Quantity * x.Rate)
        //        })
        //        .ToListAsync();

        //    // =========================
        //    // KOSTAK & SUBJECT TO
        //    // =========================
        //    foreach (var row in grouped
        //        .Where(x => x.OrderCategory == (int)IPOOrderCategory.Kostak
        //                 || x.OrderCategory == (int)IPOOrderCategory.SubjectTo))
        //    {
        //        var categoryDict = row.OrderCategory == (int)IPOOrderCategory.Kostak
        //            ? response.Kostak
        //            : response.SubjectTo;

        //        var investorKey = ((IPOInvestorType)row.InvestorType).ToString();

        //        if (!categoryDict.ContainsKey(investorKey))
        //            categoryDict[investorKey] = new CategoryStatusBlock();

        //        var block = categoryDict[investorKey];

        //        var target = row.OrderType == (int)IPOOrderType.BUY
        //            ? block.Buy
        //            : block.Sell;

        //        target.Count += row.Count;
        //        target.Amount += row.Amount;
        //        target.Avg = target.Count == 0 ? 0 : target.Amount / target.Count;
        //    }

        //    // NET calculation
        //    void CalculateNet(Dictionary<string, CategoryStatusBlock> dict)
        //    {
        //        foreach (var item in dict.Values)
        //        {
        //            item.Net.Count = item.Buy.Count - item.Sell.Count;
        //            item.Net.Amount = item.Buy.Amount - item.Sell.Amount;
        //            item.Net.Avg = item.Net.Count == 0 ? 0 : item.Net.Amount / item.Net.Count;
        //        }
        //    }

        //    CalculateNet(response.Kostak);
        //    CalculateNet(response.SubjectTo);

        //    // =========================
        //    // PREMIUM
        //    // =========================
        //    foreach (var row in grouped
        //        .Where(x => x.OrderCategory == (int)IPOOrderCategory.Premium))
        //    {
        //        var target = row.OrderType == (int)IPOOrderType.BUY
        //            ? response.Premium.Buy
        //            : response.Premium.Sell;

        //        target.Count += row.Count;
        //        target.Amount += row.Amount;
        //        target.Avg = target.Count == 0 ? 0 : target.Amount / target.Count;
        //    }

        //    response.Premium.Net.Count =
        //        response.Premium.Buy.Count - response.Premium.Sell.Count;

        //    response.Premium.Net.Amount =
        //        response.Premium.Buy.Amount - response.Premium.Sell.Amount;

        //    response.Premium.Net.Avg =
        //        response.Premium.Net.Count == 0 ? 0 :
        //        response.Premium.Net.Amount / response.Premium.Net.Count;

        //    // =========================
        //    // STRIKE PRICE (CALL / PUT)
        //    // =========================
        //    var strikeGroups = grouped
        //        .Where(x => !string.IsNullOrEmpty(x.PremiumStrikePrice)&&x.PremiumStrikePrice!="Application" && x.PremiumStrikePrice != "Premium")
        //        .GroupBy(x => x.PremiumStrikePrice);

        //    foreach (var sg in strikeGroups)
        //    {
        //        var block = new StrikePriceBlock
        //        {
        //            StrikePrice = decimal.Parse(sg.Key!)
        //        };

        //        var call = sg.Where(x => x.OrderType == (int)IPOOrderType.BUY);
        //        var put = sg.Where(x => x.OrderType == (int)IPOOrderType.SELL);

        //        block.Call_TotalShare = call.Sum(x => x.Count);
        //        block.Call_Amount = call.Sum(x => x.Amount);
        //        block.Call_Avg = block.Call_TotalShare == 0 ? 0 : block.Call_Amount / block.Call_TotalShare;

        //        block.Put_TotalShare = put.Sum(x => x.Count);
        //        block.Put_Amount = put.Sum(x => x.Amount);
        //        block.Put_Avg = block.Put_TotalShare == 0 ? 0 : block.Put_Amount / block.Put_TotalShare;

        //        response.StrikePrices.Add(block);
        //    }

        //    return response;
        //}

        public async Task<IPO_BuyerOrder> GetPlaceOrderDataByIdAsync(int orderId, int companyId)
        {
            return await _context.BuyerOrders
                 .Include(o => o.BuyerMaster)
                 .Include(o => o.OrderChild)
                     .ThenInclude(c => c.Group)
                 .Where(o =>
                  o.OrderId == orderId &&
                  o.BuyerMaster.CompanyId == companyId &&
                  o.BuyerMaster.IsActive&& !o.BuyerMaster.IsDeleted && !o.IsDeleted
                  ).FirstOrDefaultAsync();
        }

        public async Task<PagedResult<IPO_PlaceOrderChild>> GetAllOrderChildrenWithSearchAsync(OrderDetailPagedRequest request, int companyId, int ipoId)
        {
            var query = _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Include(c => c.Group)
                .AsSplitQuery() // Split into separate queries to avoid cartesian explosion
                .Where(c => c.CompanyId == companyId &&
                           c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                           c.IPOOrder.BuyerMaster.IsActive && !c.IsDeleted && !c.IPOOrder.BuyerMaster.IsDeleted);

            // Global search across multiple fields (SQL Server uses case-insensitive collation by default)
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var searchValue = request.SearchValue.Trim();
                query = query.Where(c =>
                    (c.PANNumber != null && c.PANNumber.Contains(searchValue)) ||
                    (c.ClientName != null && c.ClientName.Contains(searchValue)) ||
                    (c.DematNumber != null && c.DematNumber.Contains(searchValue)) ||
                    (c.ApplicationNo != null && c.ApplicationNo.Contains(searchValue)) ||
                    (c.Group != null &&
                     c.Group.GroupName != null &&
                     c.Group.GroupName.Contains(searchValue))
                );
            }

            // Apply group filter on child table directly
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.GroupId == request.GroupId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.ChildOrderCreatedDate)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<IPO_PlaceOrderChild>(items, totalCount, request.Skip, request.PageSize);
        }
        public async Task<int> UpdateOrderAsync(EditIPOOrderRequest request, int userId)
        {
            var order = await _context.BuyerOrders
             .Include(o => o.OrderChild)
             .Include(o => o.BuyerMaster)
             .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
                return 0; //not found

            // Get IPO's PreOpenPrice for new child orders
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(i => i.Id == order.BuyerMaster.IPOId && i.CompanyId == order.CompanyId);
            var preOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

            // ============================
            // 1️ UPDATE ORDER TABLE
            // ============================
            order.OrderType = request.OrderType;
            order.OrderCategory = request.OrderCategory;
            order.InvestorType = request.InvestorType;

            // Store Rate as entered by user - no conversion
            bool isPremiumOrOption = request.OrderCategory == (int)IPOOrderCategory.Premium ||
                                     request.OrderCategory == (int)IPOOrderCategory.CALL ||
                                     request.OrderCategory == (int)IPOOrderCategory.PUT;
            order.Rate = request.Rate;
            order.EffectiveRate = isPremiumOrOption ? request.Rate : (decimal?)null;

            order.ModifiedBy = userId.ToString();
            order.ModifiedDate = DateTime.UtcNow;
            order.DateTime = request.DateTime;
            order.Remarks = request.RemarksIds;
            order.ApplicateRate = request.ApplicateRate;
            string? premiumStrikePrice = request.PremiumStrikePrice;
            if (request.OrderCategory != 4 && request.OrderCategory != 5)
            {
                premiumStrikePrice = request.ApplicateRate ? "Premium" : "Application";
            }
            order.PremiumStrikePrice = premiumStrikePrice;

            // ============================
            // 2 HANDLE QTY CHANGE
            // ============================
            int existingQty = order.OrderChild.Count;
            int newQty = request.Quantity;

            // ---------- QTY INCREASE ----------
            if (newQty > existingQty)
            {
                int addCount = newQty - existingQty;

                // Determine AllotedQty (lot size) based on InvestorType
                int? allotedQty = order.InvestorType switch
                {
                    (int)IPOInvestorType.Retail => (int?)(ipoMaster?.IPO_Retail_Lot_Size),
                    (int)IPOInvestorType.SHNI => (int?)(ipoMaster?.IPO_SHNI_Lot_Size),
                    (int)IPOInvestorType.BHNI => (int?)(ipoMaster?.IPO_BHNI_Lot_Size),
                    _ => null
                };

                for (int i = 0; i < addCount; i++)
                {
                    order.OrderChild.Add(new IPO_PlaceOrderChild
                    {
                        Quantity = 1,
                        GroupId = request.GroupId,
                        PreOpenPrice = preOpenPrice, // Set from IPO parent
                        AllotedQty = allotedQty, // Set lot size based on investor type
                        CreatedBy = userId,
                        CompanyId = order.CompanyId,
                        ChildOrderCreatedDate = DateTime.UtcNow
                    });
                }
            }

            // ---------- QTY DECREASE ----------
            if (newQty < existingQty)
            {
                int removeCount = existingQty - newQty;

                // Only PAN NULL records allowed to delete
                var deletableChildren = order.OrderChild
                    .Where(c => string.IsNullOrEmpty(c.PANNumber))
                    .OrderByDescending(c => c.POChildId)
                    .Take(removeCount)
                    .ToList();

                if (deletableChildren.Count < removeCount)
                {
                    // PAN filled children exist → not allowed
                    //throw new Exception("Cannot reduce quantity because PAN already exists");
                    return -1; // indicate PAN exists
                }

                _context.ChildPlaceOrder.RemoveRange(deletableChildren);
            }
            // ============================
            // 3️ UPDATE CHILD DATA
            // ============================
            foreach (var child in order.OrderChild)
            {
                child.GroupId = request.GroupId;
                child.ModifiedBy = userId.ToString();
                child.ModifiedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            order.Quantity = order.OrderChild.Count;

            await _context.SaveChangesAsync();
            return order.OrderId;
        }

        public async Task<bool> DeleteOrderAsync(int orderId, int userId)
        {
            var order = await _context.BuyerOrders
              .Include(o => o.OrderChild)
              .Include(o => o.BuyerMaster)
             .ThenInclude(m => m.Orders)
             .FirstOrDefaultAsync(o => o.OrderId == orderId &&!o.IsDeleted);
            if (order == null)
                return false;

            // ============================
            // 1️ SOFT DELETE CHILD
            // ============================
            foreach (var child in order.OrderChild)
            {
                child.IsDeleted = true;
                child.ModifiedBy = userId.ToString();
                child.ModifiedDate = DateTime.UtcNow;
            }

            // ============================
            // 2️ SOFT DELETE ORDER
            // ============================
            order.IsDeleted = true;
            order.ModifiedBy = userId.ToString();
            order.ModifiedDate = DateTime.UtcNow;

            // ============================
            // 3️ CHECK MASTER STATUS
            // ============================
            bool anyActiveOrder = order.BuyerMaster.Orders
                .Any(o => !o.IsDeleted && o.OrderId != orderId);

            if (!anyActiveOrder)
            {
                order.BuyerMaster.IsActive = false;
                order.BuyerMaster.ModifiedBy = userId.ToString();
                order.BuyerMaster.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<int> ResolveGroupIdAsync(string groupName, int companyId, int ipoId, Dictionary<string, int> cache)
        {
            if (cache.TryGetValue(groupName, out int cachedId))
                return cachedId;

            var group = await _context.IPO_GroupMaster
                .FirstOrDefaultAsync(x =>
                    x.GroupName == groupName &&
                    x.CompanyId == companyId);

            if (group == null)
            {
                group = new IPO_GroupMaster
                {
                    GroupName = groupName,
                    CompanyId = companyId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.IPO_GroupMaster.Add(group);
                await _context.SaveChangesAsync();
            }

            cache[groupName] = group.IPOGroupId;
            return group.IPOGroupId;
        }
        private async Task<string> ResolveRemarkIdsAsync(string? remarks,int companyId, int userId,int ipoId, Dictionary<string, int> cache)
        {
            if (string.IsNullOrWhiteSpace(remarks))
                return string.Empty;

            var remarkNames = remarks
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var remarkIds = new List<int>();

            foreach (var name in remarkNames)
            {
                if (!cache.TryGetValue(name, out int remarkId))
                {
                    var remark = await _context.IPO_OrderRemark
                        .FirstOrDefaultAsync(x =>
                            x.Remark == name &&
                            x.CompanyId == companyId && x.IsActive);

                    if (remark == null)
                    {
                        remark = new IPO_Order_Remark
                        {
                            Remark = name,
                            CompanyId = companyId,
                            CreatedBy = userId,
                            CreatedDate = DateTime.UtcNow,
                            IsActive = true,
                            IPOId=ipoId
                        };

                        _context.IPO_OrderRemark.Add(remark);
                        await _context.SaveChangesAsync();
                    }

                    cache[name] = remark.RemarkId;
                    remarkId = remark.RemarkId;
                }

                remarkIds.Add(remarkId);
            }

            return string.Join(",", remarkIds);
        }
        public async Task<bool> BulkOrderUploadAsync(
     int ipoId,
     List<string[]> rows,
     int createdByUserId,
     int companyId,
     int? orderId)
        {
            IPO_BuyerPlaceOrderMaster? master = null;
            IPO_BuyerOrder? existingOrder = null;

            // Get IPO's PreOpenPrice for child orders
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(i => i.Id == ipoId && i.CompanyId == companyId);
            var preOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

            // =========================
            // 1️ CREATE MASTER / LOAD ORDER
            // =========================
            if (orderId == null)
            {
                master = new IPO_BuyerPlaceOrderMaster
                {
                    IPOId = ipoId,
                    CompanyId = companyId,
                    CreatedBy = createdByUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    Orders = new List<IPO_BuyerOrder>()
                };
            }
            else
            {
                existingOrder = await _context.BuyerOrders
                    .Include(o => o.OrderChild)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == orderId &&
                        o.CompanyId == companyId);

                if (existingOrder == null)
                    return false; // invalid orderId
            }

            if (rows == null || rows.Count == 0)
                return false;

            var groupCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var remarkCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var col in rows)
            {
                int groupId = await ResolveGroupIdAsync(col[0], companyId, ipoId, groupCache);

                int orderType = (int)Enum.Parse<IPOOrderType>(col[1], true);
                int orderCategory = (int)Enum.Parse<IPOOrderCategory>(col[2], true);
                int investorType = (int)Enum.Parse<IPOInvestorType>(col[3], true);

                int quantity = int.Parse(col[4]);
                decimal rate = decimal.Parse(col[5]);

                string? strikePrice = string.IsNullOrWhiteSpace(col[6]) ? null : col[6];

                var orderDateTime =
                    DateTime.Parse(col[7]).Date.Add(TimeSpan.Parse(col[8]));

                // =========================
                // 5️ DATETIME
                // =========================
                var date = DateTime.Parse(col[7]);
                var time = TimeSpan.Parse(col[8]);
               
                string remarkIds = await ResolveRemarkIdsAsync( col[9], companyId,createdByUserId, ipoId,remarkCache);
                // =========================
                

                // =========================
                // 6️ CREATE OR APPEND
                // =========================
                if (orderId == null)
                {
                    var order = new IPO_BuyerOrder
                    {
                        OrderType = orderType,
                        OrderCategory = orderCategory,
                        InvestorType = investorType,
                        Quantity = quantity,
                        // For CALL only: add StrikePrice to Rate (PUT stores raw rate)
                        Rate = orderCategory == (int)IPOOrderCategory.CALL
                            && !string.IsNullOrEmpty(strikePrice) && decimal.TryParse(strikePrice, out var csvSp)
                            ? rate + csvSp : rate,
                        PremiumStrikePrice = strikePrice,
                        ApplicateRate = false,
                        DateTime = orderDateTime,
                        Remarks = remarkIds,
                        CreatedBy = createdByUserId,
                        CompanyId = companyId,
                        OrderCreatedDate = DateTime.UtcNow,
                        OrderChild = new List<IPO_PlaceOrderChild>(),
                        // EffectiveRate = entered rate for Premium/CALL/PUT
                        EffectiveRate = (orderCategory == (int)IPOOrderCategory.Premium ||
                                orderCategory == (int)IPOOrderCategory.CALL ||
                                orderCategory == (int)IPOOrderCategory.PUT)
                            ? rate
                            : (decimal?)null
                    };

                    // Determine AllotedQty (lot size) based on InvestorType
                    int? csvAllotedQty = investorType switch
                    {
                        (int)IPOInvestorType.Retail => (int?)(ipoMaster?.IPO_Retail_Lot_Size),
                        (int)IPOInvestorType.SHNI => (int?)(ipoMaster?.IPO_SHNI_Lot_Size),
                        (int)IPOInvestorType.BHNI => (int?)(ipoMaster?.IPO_BHNI_Lot_Size),
                        _ => null
                    };

                    for (int i = 0; i < quantity; i++)
                    {
                        order.OrderChild.Add(new IPO_PlaceOrderChild
                        {
                            GroupId = groupId,
                            Quantity = 1,
                            PreOpenPrice = preOpenPrice, // Set from IPO parent
                            AllotedQty = csvAllotedQty, // Set lot size based on investor type
                            CreatedBy = createdByUserId,
                            CompanyId = companyId,
                            ChildOrderCreatedDate = DateTime.UtcNow
                        });
                    }

                    master!.Orders.Add(order);
                }
                else
                {
                    // Optional safety check
                    if (existingOrder!.OrderType != orderType ||
                        existingOrder.OrderCategory != orderCategory ||
                        existingOrder.InvestorType != investorType)
                    {
                        throw new InvalidOperationException(
                            "CSV order data does not match existing order.");
                    }

                    // Determine AllotedQty (lot size) based on InvestorType
                    int? existAllotedQty = existingOrder.InvestorType switch
                    {
                        (int)IPOInvestorType.Retail => (int?)(ipoMaster?.IPO_Retail_Lot_Size),
                        (int)IPOInvestorType.SHNI => (int?)(ipoMaster?.IPO_SHNI_Lot_Size),
                        (int)IPOInvestorType.BHNI => (int?)(ipoMaster?.IPO_BHNI_Lot_Size),
                        _ => null
                    };

                    for (int i = 0; i < quantity; i++)
                    {
                        existingOrder.OrderChild.Add(new IPO_PlaceOrderChild
                        {
                            GroupId = groupId,
                            Quantity = 1,
                            PreOpenPrice = preOpenPrice, // Set from IPO parent
                            AllotedQty = existAllotedQty, // Set lot size based on investor type
                            CreatedBy = createdByUserId,
                            CompanyId = companyId,
                            ChildOrderCreatedDate = DateTime.UtcNow
                        });
                    }

                    existingOrder.Quantity += quantity;
                }
            }

            // =========================
            // 8️ Save
            // =========================
            if (orderId == null)
            {
                await _dbSet.AddAsync(master!);
            }

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<byte[]?> DeletedAllOrderAsync(int ipoId, int userId, int companyId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var masters = await _context.BuyerPlaceOrderMasters
               .Include(m => m.Orders)
                   .ThenInclude(o => o.OrderChild).ThenInclude(c=>c.Group)
               .Where(x =>  x.CompanyId == companyId && x.IPOId == ipoId &&!x.IsDeleted).ToListAsync();

                if (!masters.Any())
                    return null;
                // =========================
                // COLLECT DATA FOR EXCEL
                // =========================
                var excelRows = new List<DeletedAllOrderExcelRowResponse>();

                foreach (var master in masters)
                {
                    foreach (var order in master.Orders.Where(o => !o.IsDeleted))
                    {
                        var firstChild = order.OrderChild?.FirstOrDefault();
                        var remarks=await ResolveRemarkNamesAsync(order.Remarks, ipoId, companyId);
                        excelRows.Add(new DeletedAllOrderExcelRowResponse
                       {
                                GroupName = firstChild?.Group?.GroupName ?? "-",
                                OrderCategory = ((IPOOrderCategory)order.OrderCategory).ToString(),
                                InvestorType = ((IPOInvestorType)order.InvestorType).ToString(),
                                OrderType= ((IPOOrderType)order.OrderType).ToString(),
                                Quantity = order.Quantity,
                                Rate = order.Rate,
                                PremiumStrikePrice = order.PremiumStrikePrice ?? "-",
                                Remarks = remarks ?? "-",
                                Date = order.DateTime.ToString("yyyy-MM-dd"),
                                Time = order.DateTime.ToString("HH:mm:ss")
                       });
                    }
                }

                // =========================
                // 1️ CREATE DELETE SUMMARY
                // =========================
                var totalOrders = masters .SelectMany(m => m.Orders) .Count(o => !o.IsDeleted);
                var deleteHistory = new IPO_DeleteOrderHistory
                {
                    CompanyId = companyId,
                    DeletedBy = userId,
                    TotalOrdersDeleted = totalOrders
                };
                _context.IPO_DeleteOrderHistory.Add(deleteHistory);
                await _context.SaveChangesAsync(); // HistoryId generated

                // =========================
                // 2️ BACKUP EVERYTHING
                // =========================
                foreach (var master in masters)
                {
                    _context.Add(new OrderMaster_DeletedHistory
                    {
                        BuyerMasterId = master.BuyerMasterId,
                        IPOId = master.IPOId,
                        DeleteHistoryId = deleteHistory.HistoryId,
                        CreatedBy = master.CreatedBy,
                        CompanyId = master.CompanyId,
                        CreatedDate = master.CreatedDate,
                        DeletedBy = userId
                    });

                    foreach (var order in master.Orders)
                    {
                        _context.Add(new Order_DeletedHistory
                        {
                            OrderId = order.OrderId,
                            BuyerMasterId = order.BuyerMasterId,
                            DeleteHistoryId = deleteHistory.HistoryId,
                            OrderType = order.OrderType,
                            OrderCategory = order.OrderCategory,
                            InvestorType = order.InvestorType,
                            Quantity = order.Quantity,
                            Rate = order.Rate,
                            DateTime = order.DateTime,
                            Remarks = order.Remarks,
                            DeletedBy = userId
                        });

                        foreach (var child in order.OrderChild)
                        {
                            _context.Add(new OrderChild_DeletedHistory
                            {
                                POChildId = child.POChildId,
                                OrderId = child.OrderId,
                                DeleteHistoryId = deleteHistory.HistoryId,
                                GroupId = child.GroupId,
                                PANNumber = child.PANNumber,
                                ClientName = child.ClientName,
                                DematNumber = child.DematNumber,
                                ApplicationNo = child.ApplicationNo,
                                AllotedQty = child.AllotedQty,
                                DeletedBy = userId,
                                Quantity = 1
                            });

                            child.IsDeleted = true;
                        }

                        order.IsDeleted = true;
                    }

                    master.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // =========================
                //  GENERATE EXCEL
                // =========================
                var sb = new StringBuilder();
                sb.AppendLine("Group,OrderType,OrderCategory,InvestorType,PremiumStrikePrice,Qty,Rate,Date,Time,Remarks");

                foreach (var r in excelRows)
                {
                    sb.AppendLine(
                        $"{r.GroupName},{r.OrderType},{r.OrderCategory},{r.InvestorType},{r.PremiumStrikePrice},{r.Quantity}," +
                        $"{r.Rate},{r.Date},{r.Time},{r.Remarks}");
                }
                byte[] bytes=!string.IsNullOrEmpty(sb.ToString()) ? Encoding.UTF8.GetBytes(sb.ToString()) : Array.Empty<byte>();
                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<IPO_PlaceOrderChild>> GetOrdersAsync(int ipoId, int companyId, DownloadFilterType downloadFilterType)
        {
            var query = _context.ChildPlaceOrder
             .Include(x => x.IPOOrder)
                 .ThenInclude(o => o.BuyerMaster)
             .Where(x =>
                 x.CompanyId == companyId &&
                 !x.IsDeleted &&
                 !x.IPOOrder.IsDeleted &&
                 !x.IPOOrder.BuyerMaster.IsDeleted &&
                 x.IPOOrder.BuyerMaster.IPOId == ipoId);

            if (downloadFilterType == DownloadFilterType.PendingPAN)
            {
                query = query.Where(x => x.PANNumber == null || x.PANNumber == "");
            }

            return await query.AsNoTracking().ToListAsync();
        }

        public async  Task<string> ResolveRemarkNamesAsync(string? remarkIds,int ipoId ,int companyId)
        {
            if (string.IsNullOrWhiteSpace(remarkIds))
                return string.Empty;

            var ids = remarkIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .Distinct()
                .ToList();

            var names = await _context.IPO_OrderRemark
                .Where(x =>
                    ids.Contains(x.RemarkId) &&
                    x.CompanyId == companyId &&
                    x.IsActive && x.IPOId== ipoId)
                .Select(x => x.Remark)
                .ToListAsync();

            
            return $"\"{string.Join(",", names)}\"";
        }

        public async Task<PagedResult<IPO_PlaceOrderChild>> GetClientWisePagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId)
        {
            // Get IPO Master data for this ipoId to use in Amount calculation
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(i => i.Id == ipoId && i.CompanyId == companyId);

            var query = _context.ChildPlaceOrder
                 .Include(c => c.IPOOrder)
                     .ThenInclude(o => o.BuyerMaster)
                 .Include(c => c.Group)
                 .AsQueryable();

            query = query.Where(c =>
                c.IPOOrder.BuyerMaster.IsActive &&
                c.IPOOrder.BuyerMaster.CompanyId == companyId
                && c.IPOOrder.BuyerMaster.IPOId == ipoId && !c.IsDeleted && !c.IPOOrder.BuyerMaster.IsDeleted && !c.IPOOrder.IsDeleted
            );

            // Apply global search filter
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue?.Trim().ToLower();
                int? orderTypeMatch = Enum.GetValues(typeof(IPOOrderType)).Cast<IPOOrderType>()
                .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                int? orderCategoryMatch = Enum.GetValues(typeof(IPOOrderCategory)).Cast<IPOOrderCategory>()
                    .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                int? investorTypeMatch = Enum.GetValues(typeof(IPOInvestorType)).Cast<IPOInvestorType>()
                    .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                query = query.Where(o =>
                   (o.PANNumber != null && o.PANNumber.Contains(request.SearchValue)) ||
                    (o.ClientName != null && o.ClientName.Contains(request.SearchValue)) ||
                    (o.DematNumber != null && o.DematNumber.Contains(request.SearchValue)) ||
                    (o.ApplicationNo != null && o.ApplicationNo.Contains(request.SearchValue)) ||
                     (o.Group != null &&
                    o.Group.GroupName != null &&
                     o.Group.GroupName.ToLower().Contains(request.SearchValue.ToLower()))||
                   (orderTypeMatch.HasValue && o.IPOOrder.OrderType == orderTypeMatch.Value)
                 || (orderCategoryMatch.HasValue && o.IPOOrder.OrderCategory == orderCategoryMatch.Value)
                 || (investorTypeMatch.HasValue && o.IPOOrder.InvestorType == investorTypeMatch.Value)
                 || (o.IPOOrder.PremiumStrikePrice == request.SearchValue)
             ); 
            }

            // Apply group filter on child table directly
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.GroupId == request.GroupId.Value);

            // Apply category and investor type filters
            if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
                query = query.Where(c => c.IPOOrder.OrderCategory == request.OrderCategoryId.Value);

            if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
                query = query.Where(c => c.IPOOrder.InvestorType == request.InvestorTypeId.Value);

            // Kostak/SubjectTo: show individual child rows (each child = separate row)
            // Premium/CALL/PUT: group by OrderId (one row per order, use Order Quantity as AllotedQty)
            var allData = await query
                .Select(c => new { c.OrderId, c.POChildId, c.AllotedQty, c.IPOOrder.OrderCategory, c.IPOOrder.Quantity, CreatedDate = c.IPOOrder.BuyerMaster.CreatedDate })
                .ToListAsync();

            // Kostak(1) / SubjectTo(2) children stay as individual rows
            var individualRows = allData
                .Where(c => c.OrderCategory == (int)IPOOrderCategory.Kostak || c.OrderCategory == (int)IPOOrderCategory.SubjectTo)
                .Select(c => new { c.OrderId, ChildId = c.POChildId, AllotedQty = c.AllotedQty ?? 0, c.CreatedDate, IsGrouped = false })
                .ToList();

            // Premium/CALL/PUT grouped by OrderId — use Order Quantity (not sum of children)
            var groupedRows = allData
                .Where(c => c.OrderCategory != (int)IPOOrderCategory.Kostak && c.OrderCategory != (int)IPOOrderCategory.SubjectTo)
                .GroupBy(c => c.OrderId)
                .Select(g => new { OrderId = g.Key, ChildId = g.Min(x => x.POChildId), AllotedQty = g.First().Quantity, CreatedDate = g.First().CreatedDate, IsGrouped = true })
                .ToList();

            var combined = individualRows.Concat(groupedRows)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();

            var totalCount = combined.Count;
            var paged = combined.Skip(request.Skip).Take(request.PageSize).ToList();

            var childIds = paged.Select(x => x.ChildId).ToList();
            var items = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder).ThenInclude(o => o.BuyerMaster)
                .Include(c => c.Group)
                .AsNoTracking()
                .Where(c => childIds.Contains(c.POChildId))
                .ToListAsync();

            // Set aggregated AllotedQty for grouped rows
            foreach (var item in items)
            {
                var p = paged.FirstOrDefault(x => x.ChildId == item.POChildId);
                if (p != null && p.IsGrouped) item.AllotedQty = p.AllotedQty;
            }

            // Maintain sort order
            var orderedChildIds = paged.Select(x => x.ChildId).ToList();
            items = items.OrderBy(c => orderedChildIds.IndexOf(c.POChildId)).ToList();

            return new PagedResult<IPO_PlaceOrderChild>(items, totalCount, request.Skip, request.PageSize);
        }

        public async Task<decimal> GetClientWiseBillingTotalAsync(OrderDetailFilterRequest request, int companyId, int ipoId)
        {
            // Get IPO Master data directly for this ipoId
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(i => i.Id == ipoId && i.CompanyId == companyId);
            var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
            var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

            var query = _context.ChildPlaceOrder
                 .Include(c => c.IPOOrder)
                     .ThenInclude(o => o.BuyerMaster)
                 .Include(c => c.Group)
                 .AsQueryable();

            query = query.Where(c =>
                c.IPOOrder.BuyerMaster.IsActive &&
                c.IPOOrder.BuyerMaster.CompanyId == companyId
                && c.IPOOrder.BuyerMaster.IPOId == ipoId && !c.IsDeleted && !c.IPOOrder.BuyerMaster.IsDeleted && !c.IPOOrder.IsDeleted
            );

            // Apply same filters as GetClientWisePagedListAsync (excluding pagination)
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue?.Trim().ToLower();
                int? orderTypeMatch = Enum.GetValues(typeof(IPOOrderType)).Cast<IPOOrderType>()
                .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                int? orderCategoryMatch = Enum.GetValues(typeof(IPOOrderCategory)).Cast<IPOOrderCategory>()
                    .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                int? investorTypeMatch = Enum.GetValues(typeof(IPOInvestorType)).Cast<IPOInvestorType>()
                    .Where(e => e.ToString().ToLower().Contains(search)).Select(e => (int)e).FirstOrDefault();

                query = query.Where(o =>
                   (o.PANNumber != null && o.PANNumber.Contains(request.SearchValue)) ||
                    (o.ClientName != null && o.ClientName.Contains(request.SearchValue)) ||
                    (o.DematNumber != null && o.DematNumber.Contains(request.SearchValue)) ||
                    (o.ApplicationNo != null && o.ApplicationNo.Contains(request.SearchValue)) ||
                     (o.Group != null &&
                    o.Group.GroupName != null &&
                     o.Group.GroupName.ToLower().Contains(request.SearchValue.ToLower()))||
                   (orderTypeMatch.HasValue && o.IPOOrder.OrderType == orderTypeMatch.Value)
                 || (orderCategoryMatch.HasValue && o.IPOOrder.OrderCategory == orderCategoryMatch.Value)
                 || (investorTypeMatch.HasValue && o.IPOOrder.InvestorType == investorTypeMatch.Value)
                 || (o.IPOOrder.PremiumStrikePrice == request.SearchValue)
             );
            }

            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.GroupId == request.GroupId.Value);

            if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
                query = query.Where(c => c.IPOOrder.OrderCategory == request.OrderCategoryId.Value);

            if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
                query = query.Where(c => c.IPOOrder.InvestorType == request.InvestorTypeId.Value);

            // Premium/CALL/PUT: group by OrderId, use Order.Quantity (matches display)
            // Kostak/SubjectTo: sum per child row (each child has its own amount)
            var items = await query.ToListAsync();

            // Kostak/SubjectTo: per-child calculation
            var kostakTotal = items
                .Where(c => c.IPOOrder.OrderCategory == (int)IPOOrderCategory.Kostak ||
                            c.IPOOrder.OrderCategory == (int)IPOOrderCategory.SubjectTo)
                .Sum(c =>
                {
                    var order = c.IPOOrder;
                    var allotedQty = c.AllotedQty ?? 0;
                    var preOpenPrice = c.PreOpenPrice > 0 ? c.PreOpenPrice : ipoPreOpenPrice;
                    decimal amount = allotedQty == 0 ? 0 : (preOpenPrice - ipoPrice) * allotedQty - order.Rate;
                    if (order.OrderType == (int)IPOOrderType.SELL)
                        amount = -amount;
                    return amount;
                });

            // Premium/CALL/PUT: per-order calculation using Order.Quantity
            var premOptTotal = items
                .Where(c => c.IPOOrder.OrderCategory == (int)IPOOrderCategory.Premium ||
                            c.IPOOrder.OrderCategory == (int)IPOOrderCategory.CALL ||
                            c.IPOOrder.OrderCategory == (int)IPOOrderCategory.PUT)
                .GroupBy(c => c.OrderId)
                .Sum(g =>
                {
                    var first = g.First();
                    var order = first.IPOOrder;
                    var qty = order.Quantity;
                    var preOpenPrice = first.PreOpenPrice > 0 ? first.PreOpenPrice : ipoPreOpenPrice;
                    var orderCategory = order.OrderCategory;
                    var rate = order.EffectiveRate.HasValue
                        ? order.EffectiveRate.Value
                        : order.Rate;

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
                    return amount;
                });

            var total = kostakTotal + premOptTotal;

            return total;
        }

        public async Task<List<IPO_PlaceOrderChild>> GetGroupWiseBillingListAsync(GroupWiseBillingRequest request, int companyId, int ipoId)
        {
            var query = _context.ChildPlaceOrder
               .Include(c => c.IPOOrder)
                   .ThenInclude(o => o.BuyerMaster)
               .Include(c => c.Group)
               .Where(c => c.CompanyId == companyId &&
                          c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                          c.IPOOrder.BuyerMaster.IsActive && !c.IsDeleted && !c.IPOOrder.BuyerMaster.IsDeleted);

            // Global search across multiple fields
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var searchLower = request.SearchValue.ToLower();
                query = query.Where(c =>
                    (c.Group != null &&
                     c.Group.GroupName != null &&
                     c.Group.GroupName.ToLower().Contains(searchLower))
                );
            }
            var totalCount = await query.CountAsync();
            return await query.ToListAsync();
        }

        public async Task<PagedResult<IPO_PlaceOrderChild>> GetOrderDetailPagedListByOrderIdAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType, int orderId)
        {
            var query = _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Include(c => c.Group)
                .AsQueryable();

            query = query.Where(c => c.IPOOrder.BuyerMaster.IsActive &&
                c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                c.IPOOrder.OrderType == orderType &&
                c.IPOOrder.BuyerMaster.IPOId == ipoId && !c.IsDeleted && !c.IPOOrder.BuyerMaster.IsDeleted && !c.IPOOrder.IsDeleted &&
                new[] { 1, 2 }.Contains(c.IPOOrder.OrderCategory) &&
                new[] { 1, 2, 3 }.Contains(c.IPOOrder.InvestorType)
            );
            if (orderId>0)
            {
                query = query.Where(c => c.IPOOrder.OrderId == orderId && c.OrderId == orderId);
            }
            // Apply global search filter
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                query = query.Where(c =>
                    (c.PANNumber != null && c.PANNumber.Contains(request.SearchValue)) ||
                    (c.ClientName != null && c.ClientName.Contains(request.SearchValue)) ||
                    (c.DematNumber != null && c.DematNumber.Contains(request.SearchValue)) ||
                    (c.ApplicationNo != null && c.ApplicationNo.Contains(request.SearchValue))
                );
            }

            // Apply group filter on child table directly
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.GroupId == request.GroupId.Value);

            // Apply category and investor type filters
            if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
                query = query.Where(c => c.IPOOrder.OrderCategory == request.OrderCategoryId.Value);

            if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
                query = query.Where(c => c.IPOOrder.InvestorType == request.InvestorTypeId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue.Trim().ToLower();

                int? orderTypeMatch = Enum.GetValues(typeof(IPOOrderType))
                    .Cast<IPOOrderType>()
                    .Where(e => e.ToString().ToLower().Contains(search))
                    .Select(e => (int?)e)
                    .FirstOrDefault();

                int? orderCategoryMatch = Enum.GetValues(typeof(IPOOrderCategory))
                    .Cast<IPOOrderCategory>()
                    .Where(e => e.ToString().ToLower().Contains(search))
                    .Select(e => (int?)e)
                    .FirstOrDefault();

                int? investorTypeMatch = Enum.GetValues(typeof(IPOInvestorType))
                    .Cast<IPOInvestorType>()
                    .Where(e => e.ToString().ToLower().Contains(search))
                    .Select(e => (int?)e)
                    .FirstOrDefault();

                query = query.Where(o =>
                    o.IPOOrder != null && (
                        (orderTypeMatch.HasValue && o.IPOOrder.OrderType == orderTypeMatch.Value)
                     || (orderCategoryMatch.HasValue && o.IPOOrder.OrderCategory == orderCategoryMatch.Value)
                     || (investorTypeMatch.HasValue && o.IPOOrder.InvestorType == investorTypeMatch.Value)
                     || (o.IPOOrder.PremiumStrikePrice != null &&
                         o.IPOOrder.PremiumStrikePrice.ToLower().Contains(search))
                     || (o.IPOOrder.OrderChild.Any(c =>
                            c.Group != null &&
                            c.Group.GroupName.ToLower().Contains(search)))
                    )
                );
            }

            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.GroupId == request.GroupId.Value); 
            if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
                query = query.Where(o => o.IPOOrder != null && o.IPOOrder.OrderCategory == request.OrderCategoryId.Value);  
            if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
                query = query.Where(o => o.IPOOrder != null && o.IPOOrder.InvestorType == request.InvestorTypeId.Value); 

            // Get total count before pagination
            var totalCount = await query.CountAsync();
            var pendingPanApplications = await query.CountAsync(c => string.IsNullOrEmpty(c.PANNumber));
            // Order and apply pagination
            query = query.OrderByDescending(c => c.IPOOrder.BuyerMaster.CreatedDate)
                         .Skip(request.Skip)
                         .Take(request.PageSize);

            var items = await query.ToListAsync();
            foreach (var order in items)
            {
                string remark = order.IPOOrder.Remarks ?? "";
                if (!string.IsNullOrEmpty(remark))
                {
                    var remarkIds = remark.Split(',')
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
                    if (remarkIds.Any())
                    {
                        var remarkNames = await _context.IPO_OrderRemark
                      .Where(r => remarkIds.Contains(r.RemarkId) && r.CompanyId == companyId && r.IsActive)
                      .Select(r => r.Remark)
                      .ToListAsync();

                        order.IPOOrder.Remarks = string.Join(", ", remarkNames);
                    }


                }
                else
                {
                    order.IPOOrder.Remarks = "-";
                }
            }

            var result = new PagedResult<IPO_PlaceOrderChild>(
                items,
                totalCount,
                request.Skip,
                request.PageSize
            );

            result.Extras = new Dictionary<string, int>
            {
                { "totalApplications", totalCount },
                { "pendingPanApplications", pendingPanApplications }
            };
            return result;
        }

        public async Task<bool> UpdateChildPreOpenPriceAsync(int poChildId, decimal preOpenPrice, int companyId, int userId)
        {
            var child = await _context.ChildPlaceOrder
                .FirstOrDefaultAsync(c => c.POChildId == poChildId && c.CompanyId == companyId && !c.IsDeleted);

            if (child == null)
                return false;

            child.PreOpenPrice = preOpenPrice;
            child.ModifiedBy = userId.ToString();
            child.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> UpdateOrderChildrenPreOpenPriceAsync(int orderId, decimal preOpenPrice, int companyId, int userId)
        {
            var children = await _context.ChildPlaceOrder
                .Where(c => c.OrderId == orderId &&
                           c.CompanyId == companyId &&
                           !c.IsDeleted)
                .ToListAsync();

            foreach (var child in children)
            {
                child.PreOpenPrice = preOpenPrice;
                child.ModifiedBy = userId.ToString();
                child.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return children.Count;
        }

        public async Task<int> UpdateAllChildrenPreOpenPriceAsync(int ipoId, decimal preOpenPrice, int companyId, int userId)
        {
            var now = DateTime.UtcNow;
            var userIdStr = userId.ToString();

            // Bulk UPDATE children — no load into memory needed
            var updatedChildren = await _context.ChildPlaceOrder
                .Where(c => c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                            c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                            c.IPOOrder.BuyerMaster.IsActive &&
                            !c.IsDeleted &&
                            !c.IPOOrder.IsDeleted &&
                            !c.IPOOrder.BuyerMaster.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.PreOpenPrice, preOpenPrice)
                    .SetProperty(c => c.ModifiedBy, userIdStr)
                    .SetProperty(c => c.ModifiedDate, now));

            // Bulk UPDATE orders: only Premium/CALL/PUT where EffectiveRate is not yet set
            if (preOpenPrice > 0)
            {
                var premiumCategories = new[]
                {
                    (int)IPOOrderCategory.Premium,
                    (int)IPOOrderCategory.CALL,
                    (int)IPOOrderCategory.PUT
                };

                await _context.BuyerOrders
                    .Where(o => o.BuyerMaster.IPOId == ipoId &&
                                o.BuyerMaster.CompanyId == companyId &&
                                o.BuyerMaster.IsActive &&
                                !o.IsDeleted &&
                                !o.BuyerMaster.IsDeleted &&
                                o.EffectiveRate == null &&
                                premiumCategories.Contains(o.OrderCategory))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.EffectiveRate, o => o.Rate)
                        .SetProperty(o => o.ModifiedDate, now));
            }

            return updatedChildren;
        }

        public async Task<int> SyncChildrenPreOpenPriceFromParentAsync(int ipoId, decimal preOpenPrice, int companyId, int userId)
        {
            // Only update children where PreOpenPrice is 0 (not yet set)
            var children = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Where(c => c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                           c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                           c.IPOOrder.BuyerMaster.IsActive &&
                           !c.IsDeleted &&
                           !c.IPOOrder.IsDeleted &&
                           !c.IPOOrder.BuyerMaster.IsDeleted &&
                           c.PreOpenPrice == 0)
                .ToListAsync();

            foreach (var child in children)
            {
                child.PreOpenPrice = preOpenPrice;
                child.ModifiedBy = userId.ToString();
                child.ModifiedDate = DateTime.UtcNow;
            }

            // Also populate EffectiveRate on orders that don't have it yet
            var ordersWithoutER = await _context.BuyerOrders
                .Include(o => o.BuyerMaster)
                .Where(o => o.BuyerMaster.IPOId == ipoId &&
                           o.BuyerMaster.CompanyId == companyId &&
                           o.BuyerMaster.IsActive &&
                           !o.IsDeleted &&
                           !o.BuyerMaster.IsDeleted &&
                           o.EffectiveRate == null)
                .ToListAsync();

            foreach (var order in ordersWithoutER)
            {
                if (preOpenPrice > 0)
                {
                    bool isPremOrOpt = order.OrderCategory == (int)IPOOrderCategory.Premium ||
                                       order.OrderCategory == (int)IPOOrderCategory.CALL ||
                                       order.OrderCategory == (int)IPOOrderCategory.PUT;
                    // Rate IS the entered value — set EffectiveRate = Rate
                    order.EffectiveRate = isPremOrOpt ? order.Rate : (decimal?)null;
                    order.ModifiedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return children.Count;
        }

        public async Task<int> FixAllotedQtyFromLotSizeAsync(int ipoId, int companyId)
        {
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(i => i.Id == ipoId && i.CompanyId == companyId);
            if (ipoMaster == null) return 0;

            // Get all children with AllotedQty = 0 or null for this IPO
            var children = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Where(c => c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                           c.IPOOrder.BuyerMaster.CompanyId == companyId &&
                           c.IPOOrder.BuyerMaster.IsActive &&
                           !c.IsDeleted &&
                           !c.IPOOrder.IsDeleted &&
                           !c.IPOOrder.BuyerMaster.IsDeleted &&
                           (c.AllotedQty == null || c.AllotedQty == 0))
                .ToListAsync();

            foreach (var child in children)
            {
                int? lotSize = child.IPOOrder.InvestorType switch
                {
                    (int)IPOInvestorType.Retail => (int?)(ipoMaster.IPO_Retail_Lot_Size),
                    (int)IPOInvestorType.SHNI => (int?)(ipoMaster.IPO_SHNI_Lot_Size),
                    (int)IPOInvestorType.BHNI => (int?)(ipoMaster.IPO_BHNI_Lot_Size),
                    _ => 1 // Premium/Options - 1 per application
                };

                if (lotSize.HasValue && lotSize.Value > 0)
                {
                    child.AllotedQty = lotSize.Value;
                    child.ModifiedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return children.Count(c => c.AllotedQty > 0);
        }
    }
}
