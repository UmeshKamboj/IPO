using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Buffers;

namespace IPOClient.Repositories.Implementations
{
    public class IPOBuyerPlaceOrderRepository: BaseRepository<IPO_BuyerPlaceOrderMaster>,IIPOBuyerPlaceOrderRepository
    {
        public IPOBuyerPlaceOrderRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<int> CreateAsync(IPOBuyerPlaceOrderRequest request, int userId, int companyId)
        {
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

                var order = new IPO_BuyerOrder
                {
                    OrderType = reqOrder.OrderType,
                    OrderCategory = reqOrder.OrderCategory,
                    InvestorType = reqOrder.InvestorType,
                    PremiumStrikePrice = premiumStrikePrice,
                    ApplicateRate = reqOrder.ApplicateRate,
                    Quantity = reqOrder.Quantity,
                    Rate = reqOrder.Rate,
                    DateTime = request.DateTime,
                    CreatedBy = userId,
                    CompanyId = companyId,
                    OrderCreatedDate= DateTime.UtcNow,
                    OrderChild = new List<IPO_PlaceOrderChild>(),
                    Remarks= request.RemarksIds
                };

                // split
                for (int i = 0; i < reqOrder.Quantity; i++)
                {
                    order.OrderChild.Add(new IPO_PlaceOrderChild
                    {
                        Quantity = 1,
                        GroupId = request.GroupId, // Set GroupId from master
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
            //if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
            //    query = query.Where(o => o.OrderCategory == request.OrderCategoryId.Value);
            //if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
            //    query = query.Where(o => o.InvestorType == request.InvestorTypeId.Value);
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
            return new PagedResult<IPO_PlaceOrderChild>(items, totalCount, request.Skip, request.PageSize);
        }

        public async Task<bool> UpdateOrderDetailsAsync(UpdateOrderDetailsListRequest request, int userId)
        {
            if (request == null || request.Orders == null || !request.Orders.Any())
                return false;

            var poChildIds = request.Orders.Select(x => x.POChildId).ToList();

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
            return true;
        }
        public async Task<OrderStatusSummaryResponse> GetOrderStatusSummaryAsync(OrderStatusFilterRequest request, int companyId)
        {
            var response = new OrderStatusSummaryResponse();

            var query = _context.BuyerOrders
                .Include(o => o.BuyerMaster)
                .Include(o => o.OrderChild)
                .Where(o =>
                    o.BuyerMaster.CompanyId == companyId &&
                    o.BuyerMaster.IPOId == request.IPOId &&
                    o.BuyerMaster.IsActive && !o.BuyerMaster.IsDeleted && !o.IsDeleted)
                .AsQueryable();

            // Filter by GroupId through child table
            if (request.GroupId.HasValue && request.GroupId > 0)
                query = query.Where(o => o.OrderChild.Any(c => c.GroupId == request.GroupId));

            if (request.InvestorType.HasValue && request.InvestorType > 0)
                query = query.Where(o => o.InvestorType == request.InvestorType);
            if (request.OrderCategory.HasValue && request.OrderCategory > 0)
                query = query.Where(o => o.OrderCategory == request.OrderCategory);

            // =========================
            // GROUPED DATA
            // =========================
            var grouped = await query
                .GroupBy(o => new
                {
                    o.OrderCategory,
                    o.InvestorType,
                    o.OrderType,
                    o.PremiumStrikePrice
                })
                .Select(g => new
                {
                    g.Key.OrderCategory,
                    g.Key.InvestorType,
                    g.Key.OrderType,
                    g.Key.PremiumStrikePrice,
                    Count = g.Sum(x => x.Quantity),
                    Avg = g.Average(x => x.Rate),
                    Amount = g.Sum(x => x.Quantity * x.Rate)
                })
                .ToListAsync();
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
                .Where(c => c.CompanyId == companyId &&
                           c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                           c.IPOOrder.BuyerMaster.IsActive && !c.IsDeleted && !c.IPOOrder.BuyerMaster.IsDeleted);

            // Global search across multiple fields
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var searchLower = request.SearchValue.ToLower();
                query = query.Where(c =>
                    (c.PANNumber != null && c.PANNumber.ToLower().Contains(searchLower)) ||
                    (c.ClientName != null && c.ClientName.ToLower().Contains(searchLower)) ||
                    (c.DematNumber != null && c.DematNumber.ToLower().Contains(searchLower)) ||
                    (c.ApplicationNo != null && c.ApplicationNo.ToLower().Contains(searchLower)) ||
                    (c.Group != null &&
                     c.Group.GroupName != null &&
                     c.Group.GroupName.ToLower().Contains(searchLower))
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
             .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
                return 0; //not found

            // ============================
            // 1️ UPDATE ORDER TABLE
            // ============================
            order.OrderType = request.OrderType;
            order.OrderCategory = request.OrderCategory;
            order.InvestorType = request.InvestorType;
            order.Rate = request.Rate;
            order.ModifiedBy = userId.ToString();
            order.ModifiedDate = DateTime.UtcNow;
            order.DateTime = request.DateTime;
            order.Remarks = request.RemarksIds;
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

                for (int i = 0; i < addCount; i++)
                {
                    order.OrderChild.Add(new IPO_PlaceOrderChild
                    {
                        Quantity = 1,
                        GroupId = request.GroupId,
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
        public async Task<bool> BulkOrderUploadAsync(int ipoId, List<string[]> rows, int createdByUserId, int companyId)
        {
            // =========================
            // 1️ CREATE MASTER (ONCE)
            // =========================
            var master = new IPO_BuyerPlaceOrderMaster
            {
                IPOId = ipoId,
                CompanyId = companyId,
                CreatedBy = createdByUserId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                Orders = new List<IPO_BuyerOrder>()
            };
            // Cache GroupId lookups
            var groupCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var remarkCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if( rows == null || rows.Count == 0)
                return false;
            foreach (var col in rows)
            {
                /*
                 CSV Columns
                 0 GroupName
                 1 OrderType
                 2 OrderCategory
                 3 InvestorType
                 4 Quantity
                 5 Rate
                 6 StrikePrice / Premium / Application
                 7 OrderDate
                 8 OrderTime
                 9 Remark
                */

                // =========================
                // 2️ GROUP RESOLVE (CACHED)
                // =========================
                int groupId = await ResolveGroupIdAsync(  col[0], companyId, ipoId,groupCache);
                // =========================
                // 3️ ENUM PARSING
                // =========================
                int orderType = (int)Enum.Parse<IPOOrderType>(col[1], true);
                int orderCategory = (int)Enum.Parse<IPOOrderCategory>(col[2], true);
                int investorType = (int)Enum.Parse<IPOInvestorType>(col[3], true);

                int quantity = int.Parse(col[4]);
                decimal rate = decimal.Parse(col[5]);

                // =========================
                // 4️ PREMIUM / STRIKE LOGIC
                // =========================
                bool applicateRate = false;
                string? strikePrice = null;

                if (!string.IsNullOrWhiteSpace(col[6]))
                {
                    //if (col[6].Equals("Premium", StringComparison.OrdinalIgnoreCase))
                    //    applicateRate = true;
                    //else if (col[6].Equals("Application", StringComparison.OrdinalIgnoreCase))
                    //    applicateRate = false;
                    //else
                    strikePrice = col[6];
                }

                // =========================
                // 5️ DATETIME
                // =========================
                var date = DateTime.Parse(col[7]);
                var time = TimeSpan.Parse(col[8]);
                var orderDateTime = date.Date.Add(time);
                string remarkIds = await ResolveRemarkIdsAsync( col[9], companyId,createdByUserId, ipoId,remarkCache);
                // =========================
                // 6️ CREATE ORDER
                // =========================
                var order = new IPO_BuyerOrder
                {
                    OrderType = orderType,
                    OrderCategory = orderCategory,
                    InvestorType = investorType,
                    Quantity = quantity,
                    Rate = rate,
                    PremiumStrikePrice = strikePrice,
                    ApplicateRate = applicateRate,
                    DateTime = orderDateTime,
                    Remarks = remarkIds,
                    CreatedBy = createdByUserId,
                    CompanyId = companyId,
                    OrderCreatedDate = DateTime.UtcNow,
                    OrderChild = new List<IPO_PlaceOrderChild>()
                };

                // =========================
                // 7️ CHILD ROWS
                // =========================
                for (int i = 0; i < quantity; i++)
                {
                    order.OrderChild.Add(new IPO_PlaceOrderChild
                    {
                        GroupId = groupId,
                        Quantity = 1,
                        CreatedBy = createdByUserId,
                        CompanyId = companyId,
                        ChildOrderCreatedDate = DateTime.UtcNow
                    });
                }

                master.Orders.Add(order);
            }
            // =========================
            // 8️ Save
            // =========================
            await _dbSet.AddAsync(master);
            //_context.BuyerPlaceOrderMasters.Add(master);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeletedAllOrderAsync(int ipoId, int userId, int companyId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var masters = await _context.BuyerPlaceOrderMasters
               .Include(m => m.Orders)
                   .ThenInclude(o => o.OrderChild)
               .Where(x =>  x.CompanyId == companyId && x.IPOId == ipoId &&!x.IsDeleted).ToListAsync();

                if (!masters.Any())
                    return false;

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

                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
