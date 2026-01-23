using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Implementations;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class IPOBuyerPlaceOrderService : IIPOBuyerPlaceOrderService
    {
        private readonly IIPOBuyerPlaceOrderRepository _buyerPlaceOrderRepository;
        private readonly IIPOGroupRepository _groupRepository;

        public IPOBuyerPlaceOrderService(IIPOBuyerPlaceOrderRepository buyerPlaceOrderRepository, IIPOGroupRepository groupRepository)
        {
            _buyerPlaceOrderRepository = buyerPlaceOrderRepository;
            _groupRepository = groupRepository;
        }
        public async Task<ReturnData<BuyerPlaceOrderResponse>> CreateIPOBuyerPlaceOrderAsync(IPOBuyerPlaceOrderRequest request, int createdByUserId, int companyId)
        {
            try
            {
                var ipoId = await _buyerPlaceOrderRepository.CreateAsync(request, createdByUserId, companyId);
                var createdIPO = await _buyerPlaceOrderRepository.GetByIdAsync(ipoId, companyId);

                return ReturnData<BuyerPlaceOrderResponse>.SuccessResponse(MapToIPOResponse(createdIPO!), "Buy place order successfully", 201);
            }
            catch (Exception ex)
            {
                return ReturnData<BuyerPlaceOrderResponse>.ErrorResponse($"Error buy place order: {ex.Message}", 500);
            }
        }
        public async Task<ReturnData<BuyerPlaceOrderResponse>> GetPlaceOrderByIdAsync(int masterId, int companyId)
        {
            try
            {
                var placeorderdata = await _buyerPlaceOrderRepository.GetByIdAsync(masterId, companyId);

                return ReturnData<BuyerPlaceOrderResponse>.SuccessResponse(MapToIPOResponse(placeorderdata!), "Place order data retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<BuyerPlaceOrderResponse>.ErrorResponse($"Error place order: {ex.Message}", 500);
            }
        }
        public async Task<ReturnData<List<BuyerOrderResponse>>> GetTopFivePlaceOrderListAsync(int ipoId, int companyId)
        {
            try
            {
                var orders = await _buyerPlaceOrderRepository.GetTopFivePlaceOrderListAsync(ipoId, companyId);

                var response = new List<BuyerOrderResponse>();

                foreach (var (order, index) in orders.Select((o, i) => (o, i)))
                {
                    // Get GroupId from first child (all children have same GroupId)
                    var firstChild = order.OrderChild?.FirstOrDefault();
                    var groupId = firstChild?.GroupId ?? 0;
                    var group = groupId > 0 ? await _groupRepository.GetByIdAsync(groupId, companyId) : null;

                    response.Add(new BuyerOrderResponse
                    {
                        SrNo = index + 1,
                        OrderId= order.OrderId,
                        BuyerMasterId=order.BuyerMaster.BuyerMasterId,
                        GroupName = group?.GroupName ?? "-",
                        OrderTypeName = ((IPOOrderType)order.OrderType).ToString(),
                        OrderCategoryName = ((IPOOrderCategory)order.OrderCategory).ToString(),
                        InvestorTypeName = ((IPOInvestorType)order.InvestorType).ToString(),
                        PremiumStrikePrice = order.PremiumStrikePrice ?? "-",
                        Quantity = order.Quantity,
                        Rate = order.Rate,
                        DateTime = order.DateTime,
                        Remark=order.Remarks
                    });
                }


                return ReturnData<List<BuyerOrderResponse>>.SuccessResponse(response,"Top 5 buyer place orders retrieved successfully",200);

            }
            catch (Exception ex)
            {
                return ReturnData<List<BuyerOrderResponse>>.ErrorResponse($"Error retrieving buyer place order: {ex.Message}", 500);
            }
        }
        public async Task<ReturnData<BuyerOrderResponse>> GetPlaceOrderDataByIdAsync(int orderId, int companyId)
        {
            try
            {
                var order = await _buyerPlaceOrderRepository.GetPlaceOrderDataByIdAsync(orderId, companyId);
                if (order == null)
                    return ReturnData<BuyerOrderResponse>
                        .ErrorResponse("Order not found", 404);
                // Get GroupId from first child (all children have same GroupId)
                var firstChild = order.OrderChild?.FirstOrDefault();
                var response = new BuyerOrderResponse
                {
                    SrNo = 1, // single record
                    OrderId = order.OrderId,
                    BuyerMasterId = order.BuyerMaster.BuyerMasterId,
                    GroupName = firstChild?.Group?.GroupName ?? "-",
                    OrderTypeName = ((IPOOrderType)order.OrderType).ToString(),
                    OrderCategoryName = ((IPOOrderCategory)order.OrderCategory).ToString(),
                    InvestorTypeName = ((IPOInvestorType)order.InvestorType).ToString(),
                    PremiumStrikePrice = order.PremiumStrikePrice ?? "-",
                    Quantity = order.Quantity,
                    Rate = order.Rate,
                    DateTime = order.DateTime,
                    OrderCategory=order.OrderCategory,
                    OrderType=order.OrderType,
                    InvestorType=order.InvestorType, 
                    Remark=order.Remarks
                    GroupId=firstChild?.GroupId ?? 0
                };

                return ReturnData<BuyerOrderResponse>.SuccessResponse(response, "Order retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<BuyerOrderResponse>.ErrorResponse($"Error retrieving place order data: {ex.Message}", 500);
            }
        }
        public async Task<ReturnData<List<BuyerOrderResponse>>> GetOrderListAsync(OrderListRequest request, int companyId, int ipoId)
        {
            try
            {
                var orders = await _buyerPlaceOrderRepository.GetOrderListAsync(request, companyId, ipoId);
                var responses = orders
               .Select((order, index) => MapToOrderResponse(
                 order,
                 srNo: index + 1
                   ))
                 .ToList();

                return ReturnData<List<BuyerOrderResponse>>.SuccessResponse(responses, "Orders retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<List<BuyerOrderResponse>>.ErrorResponse($"Error retrieving orders: {ex.Message}", 500);
            }
        }
        public async Task<ReturnData<PagedResult<BuyerOrderResponse>>> GetOrderDetailPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType)
        {
            try
            {
                var pagedResult = await _buyerPlaceOrderRepository.GetOrderDetailPagedListAsync(request, companyId, ipoId, orderType);

                var responses = pagedResult.Items?
                    .Select((order, index) => MapToOrderDetailResponse(
                        order,
                        srNo: request.Skip + index + 1
                    ))
                    .ToList() ?? new List<BuyerOrderResponse>();

                var result = new PagedResult<BuyerOrderResponse>(responses, pagedResult.TotalCount, request.Skip, request.PageSize);
                return ReturnData<PagedResult<BuyerOrderResponse>>.SuccessResponse(result, "Order details retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<BuyerOrderResponse>>.ErrorResponse($"Error retrieving order details: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> UpdateOrderDetailsAsync(UpdateOrderDetailsListRequest request, int modifiedByUserId)
        {
            try
            {
                var success = await _buyerPlaceOrderRepository.UpdateOrderDetailsAsync(request, modifiedByUserId);
                if (!success)
                    return ReturnData.ErrorResponse("Order details not found or inactive", 404);

                return ReturnData.SuccessResponse("Order details updated successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error updating orderdetail: {ex.Message}",500);
            }
        }
        public async Task<ReturnData<OrderStatusSummaryResponse>> GetOrderStatusSummaryAsync(OrderStatusFilterRequest request, int companyId)
        {
            try
            {
                var data = await _buyerPlaceOrderRepository.GetOrderStatusSummaryAsync(request, companyId);
                return ReturnData<OrderStatusSummaryResponse>.SuccessResponse(data, "Order status retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<OrderStatusSummaryResponse>.ErrorResponse($"Error order status: {ex.Message}", 500);
            }

        }

        // MAP ENTITY TO RESPONSE DTO
        private BuyerPlaceOrderResponse MapToIPOResponse(IPO_BuyerPlaceOrderMaster buyer)
        {
            // Get GroupId from first child of first order (all children have same GroupId)
            var firstChild = buyer.Orders?.FirstOrDefault()?.OrderChild?.FirstOrDefault();
            return new BuyerPlaceOrderResponse
            {
                BuyerMasterId = buyer.BuyerMasterId,
                IPOId = buyer.IPOId,
                GroupId = firstChild?.GroupId ?? 0,
                Orders = buyer.Orders.Select(o => new BuyerOrderResponse
                {
                    OrderId = o.OrderId,
                    OrderType = o.OrderType,
                    OrderCategory = o.OrderCategory,
                    InvestorType = o.InvestorType,
                    PremiumStrikePrice = o.PremiumStrikePrice,
                    Quantity = o.Quantity,
                    Rate = o.Rate,
                    DateTime = o.DateTime
                }).ToList()
            };

        }

        private BuyerOrderResponse MapToOrderDetailResponse(IPO_PlaceOrderChild child, int srNo)
        {
            var order = child.IPOOrder;
            var master = order.BuyerMaster;

            return new BuyerOrderResponse
            {
                SrNo = srNo,
                POChildId = child.POChildId,
                OrderId = order.OrderId,
                BuyerMasterId = master.BuyerMasterId,

                GroupName = child.Group?.GroupName,

                OrderTypeName = ((IPOOrderType)order.OrderType).ToString(),
                OrderCategoryName = ((IPOOrderCategory)order.OrderCategory).ToString(),
                InvestorTypeName = ((IPOInvestorType)order.InvestorType).ToString(),

                PremiumStrikePrice = order.PremiumStrikePrice ?? "-",
                Quantity = order.Quantity,
                Rate = order.Rate,
                DateTime = order.DateTime,

                // SUB-CHILD FIELDS
                PanNumber = child.PANNumber ?? "",
                ClientName = child.ClientName ?? "",
                AllotedQty = child.AllotedQty ?? 0,
                DematNumber = child.DematNumber ?? "",
                ApplicationNumber = child.ApplicationNo ?? "",
                Remark= order.Remarks
            };

        }
        private BuyerOrderResponse MapToOrderResponse(IPO_BuyerOrder order, int srNo)
        {
            // Get Group from first child (all children have same GroupId)
            var firstChild = order?.OrderChild?.FirstOrDefault();
            return new BuyerOrderResponse
            {
                SrNo = srNo,
                OrderId = order.OrderId,
                BuyerMasterId = order.BuyerMaster?.BuyerMasterId ?? 0,
                GroupName = firstChild?.Group?.GroupName,
                OrderTypeName = ((IPOOrderType)order.OrderType).ToString(),
                OrderCategoryName = ((IPOOrderCategory)order.OrderCategory).ToString(),
                InvestorTypeName = ((IPOInvestorType)order.InvestorType).ToString(),
                PremiumStrikePrice = order.PremiumStrikePrice?.ToString() ?? "-",
                Quantity = order.Quantity,
                Rate = order.Rate,
                DateTime = order.DateTime
            };

        }

        public async Task<ReturnData<PagedResult<BuyerOrderResponse>>> GetAllOrderChildrenWithSearchAsync(OrderDetailPagedRequest request, int companyId, int ipoId)
        {
            try
            {
                // Get orders from IPO_BuyerOrder table (master order level, not child level)
                var pagedOrders = await _buyerPlaceOrderRepository.GetOrderPagedListAsync(request, companyId, ipoId);

                // Map IPO_BuyerOrder to BuyerOrderResponse
                var responses = pagedOrders.Items?.Select((order, index) => {
                    var firstChild = order.OrderChild?.FirstOrDefault();
                    return new BuyerOrderResponse
                    {
                        SrNo = request.Skip + index + 1,
                        OrderId = order.OrderId,
                        BuyerMasterId = order.BuyerMaster?.BuyerMasterId ?? 0,
                        GroupId = firstChild?.GroupId ?? 0,
                        GroupName = firstChild?.Group?.GroupName ?? "-",
                        OrderType = order.OrderType,
                        OrderTypeName = ((IPOOrderType)order.OrderType).ToString(),
                        OrderCategory = order.OrderCategory,
                        OrderCategoryName = ((IPOOrderCategory)order.OrderCategory).ToString(),
                        InvestorType = order.InvestorType,
                        InvestorTypeName = ((IPOInvestorType)order.InvestorType).ToString(),
                        PremiumStrikePrice = order.PremiumStrikePrice ?? "-",
                        Quantity = order.Quantity,
                        Rate = order.Rate,
                        DateTime = order.DateTime
                    };
                }).ToList() ?? new List<BuyerOrderResponse>();

                var pagedResult = new PagedResult<BuyerOrderResponse>(responses, pagedOrders.TotalCount, request.Skip, request.PageSize);
                pagedResult.Extras = pagedOrders.Extras; // Pass through extras like totalApplications, pendingPanApplications
                return ReturnData<PagedResult<BuyerOrderResponse>>.SuccessResponse(pagedResult, "Orders retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<BuyerOrderResponse>>.ErrorResponse($"Error retrieving orders: {ex.Message}", 500);
            }
        }


    }
}
