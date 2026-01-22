using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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
                GroupId = request.GroupId,
                CreatedBy = userId,
                CompanyId = companyId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                Orders = new List<IPO_BuyerOrder>()
            };

            foreach (var reqOrder in request.Orders)
            {
                var order = new IPO_BuyerOrder
                {
                    OrderType = reqOrder.OrderType,
                    OrderCategory = reqOrder.OrderCategory,
                    InvestorType = reqOrder.InvestorType,
                    PremiumStrikePrice = reqOrder.PremiumStrikePrice,
                    Quantity = reqOrder.Quantity,
                    Rate = reqOrder.Rate,
                    DateTime = request.DateTime,
                    CreatedBy = userId,
                    CompanyId = companyId,
                    OrderCreatedDate= DateTime.UtcNow,
                    OrderChild = new List<IPO_PlaceOrderChild>()
                };

                // split
                for (int i = 0; i < reqOrder.Quantity; i++)
                {
                    order.OrderChild.Add(new IPO_PlaceOrderChild
                    {
                        Quantity = 1,
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
                .FirstOrDefaultAsync(m => m.BuyerMasterId == id && m.IsActive && m.CompanyId == companyId);
        }

       

        public async Task<List<IPO_BuyerOrder>> GetTopFivePlaceOrderListAsync(int ipoId, int companyId)
        {
            return await _context.BuyerOrders
              .Include(o => o.BuyerMaster)
              .Include(o => o.OrderChild)
              .Where(o => o.BuyerMaster.IPOId == ipoId
                && o.BuyerMaster.CompanyId == companyId
                && o.BuyerMaster.IsActive)
               .OrderByDescending(o => o.BuyerMaster.BuyerMasterId)
               .Take(5)
               .ToListAsync();
        }
        public async Task<PagedResult<IPO_BuyerOrder>> GetOrderPagedListAsync(OrderDetailPagedRequest request, int companyId,int ipoId)
        {
            // Base query
            var query = _context.BuyerOrders
                .Include(o => o.BuyerMaster) // Parent included
                   .ThenInclude(m => m.Group)
                   .Include(o => o.OrderChild)
                .AsQueryable();
            // Only active and belong to company
            query = query.Where(o =>
                o.BuyerMaster.IsActive &&
                o.BuyerMaster.CompanyId == companyId &&
                o.BuyerMaster.IPOId == ipoId);

            // Apply global search if provided
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                query = query.Where(o =>
                 o.OrderChild.Any(c =>
                 (c.PANNumber != null && c.PANNumber.Contains(request.SearchValue)) ||
                 (c.ClientName != null && c.ClientName.Contains(request.SearchValue)) ||
                 (c.DematNumber != null && c.DematNumber.Contains(request.SearchValue)) ||
                 (c.ApplicationNo != null && c.ApplicationNo.Contains(request.SearchValue))
                 )
             );
            }

            //switch (request.ModuleName.ToLower())
            //{
            //    case "buy":
            //    case "sell":
            //        query = query.Where(o =>
            //            new[] { 1, 2 }.Contains(o.OrderCategory) &&
            //            new[] { 1, 2, 3 }.Contains(o.InvestorType));
            //        break;
            //    case "all":
            //    default:
            //        //No extra filters (treat as ALL)
            //        break;
            //}
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(o => o.BuyerMaster.GroupId == request.GroupId.Value);
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

        public async Task<List<IPO_BuyerOrder>> GetOrderListAsync(OrderDetailFilterRequest request, int companyId, int ipoId)
        {
            // Base query
            var query = _context.BuyerOrders
                .Include(o => o.BuyerMaster)
                   .ThenInclude(m => m.Group)
                   .Include(o => o.OrderChild)
                .AsQueryable();

            // Only active and belong to company
            query = query.Where(o =>
                o.BuyerMaster.IsActive &&
                o.BuyerMaster.CompanyId == companyId &&
                o.BuyerMaster.IPOId == ipoId);

            // Apply global search if provided
            //if (!string.IsNullOrWhiteSpace(request.SearchValue))
            //{
            //    query = query.Where(o =>
            //     o.OrderChild.Any(c =>
            //     (c.PANNumber != null && c.PANNumber.Contains(request.SearchValue)) ||
            //     (c.ClientName != null && c.ClientName.Contains(request.SearchValue)) ||
            //     (c.DematNumber != null && c.DematNumber.Contains(request.SearchValue)) ||
            //     (c.ApplicationNo != null && c.ApplicationNo.Contains(request.SearchValue))
            //     )
            // );
            //}

            // Apply group filter
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(o => o.BuyerMaster.GroupId == request.GroupId.Value);

            // Apply category and investor type filters if provided
            //if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
            //    query = query.Where(o => o.OrderCategory == request.OrderCategoryId.Value);
            //if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
            //    query = query.Where(o => o.InvestorType == request.InvestorTypeId.Value);

            query = query.OrderByDescending(o => o.BuyerMaster.CreatedDate);

            // Return all results without pagination
            return await query.ToListAsync();
        }

        public async Task<PagedResult<IPO_PlaceOrderChild>> GetOrderDetailPagedListAsync(OrderDetailPagedRequest request, int companyId, int ipoId, int orderType)
        {
            var query = _context.ChildPlaceOrder
         .Include(c => c.IPOOrder)
             .ThenInclude(o => o.BuyerMaster)
                 .ThenInclude(m => m.Group)
         .AsQueryable();

            query = query.Where(c =>
                c.IPOOrder.BuyerMaster.IsActive &&
                c.IPOOrder.BuyerMaster.CompanyId == companyId && c.IPOOrder.OrderType== orderType &&
                c.IPOOrder.BuyerMaster.IPOId == ipoId && new[] { 1, 2 }.Contains(c.IPOOrder.OrderCategory) && new[] { 1, 2, 3 }.Contains(c.IPOOrder.InvestorType)
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

            // Filters
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.IPOOrder.BuyerMaster.GroupId == request.GroupId.Value);

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

        public async Task<List<IPO_PlaceOrderChild>> GetOrderDetailListAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType)
        {
            var query = _context.ChildPlaceOrder
         .Include(c => c.IPOOrder)
             .ThenInclude(o => o.BuyerMaster)
                 .ThenInclude(m => m.Group)
         .AsQueryable();

            query = query.Where(c =>
                c.IPOOrder.BuyerMaster.IsActive &&
                c.IPOOrder.BuyerMaster.CompanyId == companyId && c.IPOOrder.OrderType == orderType &&
                c.IPOOrder.BuyerMaster.IPOId == ipoId && new[] { 1, 2 }.Contains(c.IPOOrder.OrderCategory) && new[] { 1, 2, 3 }.Contains(c.IPOOrder.InvestorType)
            );

            // Apply search filter
            //if (!string.IsNullOrWhiteSpace(request.SearchValue))
            //{
            //    query = query.Where(c =>
            //        (c.PANNumber != null && c.PANNumber.Contains(request.SearchValue)) ||
            //        (c.ClientName != null && c.ClientName.Contains(request.SearchValue)) ||
            //        (c.DematNumber != null && c.DematNumber.Contains(request.SearchValue)) ||
            //        (c.ApplicationNo != null && c.ApplicationNo.Contains(request.SearchValue))
            //    );
            //}

            // Apply group filter
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
                query = query.Where(c => c.IPOOrder.BuyerMaster.GroupId == request.GroupId.Value);

            //// Apply category and investor type filters if provided
            //if (request.OrderCategoryId.HasValue && request.OrderCategoryId.Value > 0)
            //    query = query.Where(c => c.IPOOrder.OrderCategory == request.OrderCategoryId.Value);

            //if (request.InvestorTypeId.HasValue && request.InvestorTypeId.Value > 0)
            //    query = query.Where(c => c.IPOOrder.InvestorType == request.InvestorTypeId.Value);

            query = query.OrderByDescending(c => c.IPOOrder.BuyerMaster.CreatedDate);

            // Return all results without pagination
            return await query.ToListAsync();
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

       
    }
}
