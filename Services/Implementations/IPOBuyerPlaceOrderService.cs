using Azure;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Implementations;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic.FileIO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace IPOClient.Services.Implementations
{
    public class IPOBuyerPlaceOrderService : IIPOBuyerPlaceOrderService
    {
        private readonly IIPOBuyerPlaceOrderRepository _buyerPlaceOrderRepository;
        private readonly IIPOGroupRepository _groupRepository;
        private readonly IIPORepository _ipoRepository;

        public IPOBuyerPlaceOrderService(IIPOBuyerPlaceOrderRepository buyerPlaceOrderRepository, IIPOGroupRepository groupRepository, IIPORepository ipoRepository)
        {
            _buyerPlaceOrderRepository = buyerPlaceOrderRepository;
            _groupRepository = groupRepository;
            _ipoRepository = ipoRepository;
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
                    Remark=order.Remarks,
                    GroupId=firstChild?.GroupId ?? 0,
                    ApplicateRate=order.ApplicateRate 
                };
                response.OrderCategoryOptions = GetOrderCategoryOptions(response.OrderCategory);
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

                var result = new PagedResult<BuyerOrderResponse>(responses, pagedResult.TotalCount, request.Skip, request.PageSize)
                {
                    Extras = pagedResult.Extras // Pass through extras like totalApplications, pendingPanApplications
                };
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
        public async Task<ReturnData> UpdateOrderAsync(EditIPOOrderRequest request, int modifiedByUserId)
        {
            try
            {
                var orderId = await _buyerPlaceOrderRepository.UpdateOrderAsync(request, modifiedByUserId);
                if (orderId == 0)
                {
                    return ReturnData.ErrorResponse("Order details not found or inactive", 404);
                }
                else if (orderId == -1)
                {
                    return ReturnData.ErrorResponse("Cannot reduce quantity because PAN already exists", 404);
                }
                else
                {
                    return ReturnData.SuccessResponse("Order updated successfully", 200);
                }

            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error updating order: {ex.Message}", 500);
            }
        }
        public async Task<ReturnData> DeleteOrderAsync(int orderId, int userId)
        {
            try
            {
                var success = await _buyerPlaceOrderRepository.DeleteOrderAsync(orderId, userId);
                if (!success)
                {
                    return ReturnData.ErrorResponse("Order not found", 404);
                }
                return ReturnData.SuccessResponse("Order deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error deleting order: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> BulkOrderUploadAsync(int ipoId, IFormFile file, int createdByUserId, int companyId)
        {
            try
            {
                var rows = new List<string[]>();
                using var stream = file.OpenReadStream();
                using var parser = new TextFieldParser(stream);

                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // 🔹 Skip header
                if (!parser.EndOfData)
                    parser.ReadLine();

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    if (fields == null || fields.Length == 0)
                        continue;

                    rows.Add(fields); 
                }

                if (!rows.Any())
                    return ReturnData.ErrorResponse("CSV file is empty", 400);

                var success = await _buyerPlaceOrderRepository.BulkOrderUploadAsync(ipoId, rows, createdByUserId, companyId);
                return success
                    ? ReturnData.SuccessResponse("Bulk order uploaded successfully", 201)
                    : ReturnData.ErrorResponse("Bulk order upload failed", 500);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error uploading bulk order: {ex.Message}", 500);
            }
            

        }
        public async Task<ReturnData<FileResponse>> DeleteAllOrderAsync(int ipoId, int userId, int companyId)
        {
            try
            {
                byte[]? bytes = await _buyerPlaceOrderRepository.DeletedAllOrderAsync(ipoId, userId, companyId);
                var ipo = await _ipoRepository.GetByIdAsync(ipoId, companyId);
               

                if (bytes!=null)
                {
                    var file = new FileResponse
                    {
                        Bytes = bytes,
                        ContentType = "text/csv",
                        FileName = $"{ipo?.IPOName ?? ""}_DeletedOrders_{DateTime.Now:yyyyMMddHH}.csv"
                    };
                    return ReturnData<FileResponse>.SuccessResponse(file, "Order deleted successfully", 200);
                }
                else
                {
                    return ReturnData<FileResponse>.ErrorResponse("Order not found", 404);
                }
                    
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"Error deleting order: {ex.Message}", 500);
            }
        }


        public async Task<ReturnData<FileResponse>> DownloadSingleFileAsync(int ipoId, int companyId, DownloadFilterType downloadFilterType)
        {
            try
            {
                var data = await _buyerPlaceOrderRepository.GetOrdersAsync(ipoId, companyId, downloadFilterType);
                var sb = new StringBuilder();
                sb.AppendLine("Group,IPO Type,Investor Type,Rate,PANNumber,ClientName,AllotedQty,DemantNumber,ApplicationNumber,OrderDate,OrderTime,Remark");
                foreach (var x in data)
                {
                    var group = await _groupRepository.GetByIdAsync(x.GroupId, companyId);
                    var orderDate = x.IPOOrder.DateTime.ToString("dd-MM-yyyy");
                    var orderTime = x.IPOOrder.DateTime.ToString("HH:mm");
                    var remarkNames = await _buyerPlaceOrderRepository.ResolveRemarkNamesAsync(x.IPOOrder.Remarks, ipoId,companyId);
                    sb.AppendLine(
                        $"{group?.GroupName??"-"},{((IPOOrderCategory)x.IPOOrder.OrderCategory).ToString()},{((IPOInvestorType)x.IPOOrder.InvestorType).ToString()},{x.IPOOrder.Rate},{x.PANNumber ?? ""},{x.ClientName??""},{ x.AllotedQty },{ x.DematNumber ?? ""},{ x.ApplicationNo ?? ""},{ orderDate},{ orderTime},{ remarkNames ?? "-"}");
               
                }
                var csv = Encoding.UTF8.GetBytes(sb.ToString());
                var ipo = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                var fileprefix = downloadFilterType == DownloadFilterType.All ? "-AllRecords" : "";
                var fileResponse = new FileResponse
                {
                    Bytes = csv,
                    ContentType = "text/csv",
                    FileName = $"{ipo?.IPOName ?? ""}-OrderDetail{fileprefix}.csv"
                };
                return  ReturnData<FileResponse>.SuccessResponse(fileResponse, "File downloaded", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"Error downloading file: {ex.Message}", 500);
            }   
            
        }

        public async Task<ReturnData<FileResponse>> DownloadGroupWiseFileAsync(int ipoId, int companyId, DownloadFilterType downloadFilterType)
        {
            try
            {
                var ipo = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                var data = await _buyerPlaceOrderRepository.GetOrdersAsync(ipoId, companyId, downloadFilterType);
                using var ms = new MemoryStream();
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var grp in data.GroupBy(x => x.GroupId))
                    {
                        var group_ = await _groupRepository.GetByIdAsync(grp.Key, companyId);
                        var entry = zip.CreateEntry($"{ipo?.IPOName ?? ""}_{group_?.GroupName ?? ""}.csv");
                        using var sw = new StreamWriter(entry.Open());

                        sw.WriteLine("Group,IPO Type,Investor Type,Rate,PANNumber,ClientName,AllotedQty,DemantNumber,ApplicationNumber,OrderDate,OrderTime,Remark");
                        foreach (var x in grp)
                        {
                            var group = await _groupRepository.GetByIdAsync(x.GroupId, companyId);
                            var orderDate = x.IPOOrder.DateTime.ToString("dd-MM-yyyy");
                            var orderTime = x.IPOOrder.DateTime.ToString("HH:mm");
                            var remarkNames = await _buyerPlaceOrderRepository.ResolveRemarkNamesAsync(x.IPOOrder.Remarks, ipoId, companyId);
                            sw.WriteLine((
                            $"{group?.GroupName ?? "-"},{((IPOOrderCategory)x.IPOOrder.OrderCategory).ToString()},{((IPOInvestorType)x.IPOOrder.InvestorType).ToString()},{x.IPOOrder.Rate},{x.PANNumber ?? ""},{x.ClientName ?? ""},{x.AllotedQty},{x.DematNumber ?? ""},{x.ApplicationNo ?? ""},{orderDate},{orderTime},{remarkNames ?? "-"}"));
                        }
                        sw.Flush();
                    }
                }
                ms.Position = 0;
                var dateStamp = DateTime.Now.ToString("yyyyMMddHHmm");
                var fileResponse = new FileResponse
                {
                    Bytes = ms.ToArray(),
                    ContentType = "application/zip",
                    FileName = $"{ipo?.IPOName ?? ""}_GroupWiseOrders_{dateStamp}.zip"
                };
                return ReturnData<FileResponse>.SuccessResponse(fileResponse, "File downloaded", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"Error downloading file: {ex.Message}", 500);
            }

        }
        public async Task<ReturnData<PagedResult<BuyerOrderResponse>>> GetClientWiseBillingPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId)
        {
            try
            {
                var pagedResult = await _buyerPlaceOrderRepository.GetClientWisePagedListAsync(request, companyId, ipoId);

                var responses = pagedResult.Items?
                    .Select((order, index) => MapToOrderDetailResponse(
                        order,
                        srNo: request.Skip + index + 1
                    ))
                    .ToList() ?? new List<BuyerOrderResponse>();

                var result = new PagedResult<BuyerOrderResponse>(responses, pagedResult.TotalCount, request.Skip, request.PageSize);
                return ReturnData<PagedResult<BuyerOrderResponse>>.SuccessResponse(result, "Client wise billing retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<BuyerOrderResponse>>.ErrorResponse($"Error retrieving order details: {ex.Message}", 500);
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
                Remark = order.Remarks,
                PreOpenPrice = child.Group?.IPOMaster?.OpenIPOPrice ?? 0
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

        /// <summary>
        /// Get order category options based on category type for dropdown
        /// </summary>
        /// <param name="orderCategoryType">1 = Call/Put, 2 = Premium, 3 = Kostak/SubjectTo</param>
        public OrderCategoryOptionsResponse GetOrderCategoryOptions(int orderCategoryType)
        {
            var response = new OrderCategoryOptionsResponse();

            // Order Types - Buy/Sell are common for all categories
            response.OrderTypes = new List<DropdownOption>
            {
                new DropdownOption { Id = (int)IPOOrderType.BUY, Name = IPOOrderType.BUY.ToString() },
                new DropdownOption { Id = (int)IPOOrderType.SELL, Name = IPOOrderType.SELL.ToString() }
            };

            switch (orderCategoryType)
            {
                case 4: // Call/Put
                case 5:
                    response.OrderCategories = new List<DropdownOption>
                    {
                        new DropdownOption { Id = (int)IPOOrderCategory.CALL, Name = IPOOrderCategory.CALL.ToString() },
                        new DropdownOption { Id = (int)IPOOrderCategory.PUT, Name = IPOOrderCategory.PUT.ToString() }
                    };
                    response.InvestorTypes = new List<DropdownOption>
                    {
                        new DropdownOption { Id = (int)IPOInvestorType.OPTIONS, Name = IPOInvestorType.OPTIONS.ToString() }
                    };
                    break;

                case 3: // Premium
                    response.OrderCategories = new List<DropdownOption>
                    {
                        new DropdownOption { Id = (int)IPOOrderCategory.Premium, Name = IPOOrderCategory.Premium.ToString() }
                    };
                    response.InvestorTypes = new List<DropdownOption>
                    {
                        new DropdownOption { Id = (int)IPOInvestorType.Premium, Name = IPOInvestorType.Premium.ToString() }
                    };
                    break;

                case 2: // Kostak/SubjectTo
                case 1:
                    response.OrderCategories = new List<DropdownOption>
                    {
                        new DropdownOption { Id = (int)IPOOrderCategory.Kostak, Name = IPOOrderCategory.Kostak.ToString() },
                        new DropdownOption { Id = (int)IPOOrderCategory.SubjectTo, Name = IPOOrderCategory.SubjectTo.ToString() }
                    };
                    response.InvestorTypes = new List<DropdownOption>
                    {
                        new DropdownOption { Id = (int)IPOInvestorType.SHNI, Name = IPOInvestorType.SHNI.ToString() },
                        new DropdownOption { Id = (int)IPOInvestorType.Retail, Name = IPOInvestorType.Retail.ToString() },
                        new DropdownOption { Id = (int)IPOInvestorType.BHNI, Name = IPOInvestorType.BHNI.ToString() }
                    };
                    break;

                default:
                    // Return empty lists for invalid category type
                    break;
            }

            return response;
        }
        public async Task<ReturnData<PagedResult<GroupWiseBillingResponse>>> GetGroupWiseBillingListAsync(GroupWiseBillingRequest request, int companyId, int ipoId)
        {
            try
            {
                var data = await _buyerPlaceOrderRepository.GetGroupWiseBillingListAsync(request, companyId, ipoId);
                var groupedResult = new List<GroupWiseBillingResponse>();

                foreach (var grp in data.GroupBy(x => x.GroupId))
                {
                    var first = grp.First();

                    var res = new GroupWiseBillingResponse
                    {
                        GroupName = first.Group?.GroupName ?? "-"
                    };

                    foreach (var row in grp)
                    {
                        var order = row.IPOOrder;

                        var qty = order.OrderType == (int)IPOOrderType.BUY
                                    ? row.Quantity
                                    : -row.Quantity;

                        var amount = qty * order.Rate;

                        // ===== KOSTAK
                        if (order.OrderCategory == (int)IPOOrderCategory.Kostak)
                            FillRetailSHNI(order, res, qty, amount);

                        // ===== SUBJECT TO
                        if (order.OrderCategory == (int)IPOOrderCategory.SubjectTo)
                            FillSubjectTo(order, res, qty, amount);

                        // ===== PREMIUM
                        if (order.OrderCategory == (int)IPOOrderCategory.Premium)
                        {
                            res.Premium.Shares += qty;
                            res.Premium.Billing += amount;
                        }

                        // ===== OPTIONS
                        if (!string.IsNullOrEmpty(order.PremiumStrikePrice) &&
                            order.PremiumStrikePrice != "Application" &&
                            order.PremiumStrikePrice != "Premium")
                        {
                            if (order.OrderType == (int)IPOOrderType.BUY)
                                res.Options.CallAmount += amount;
                            else
                                res.Options.PutAmount += amount;
                        }

                        res.TotalShares += qty;
                        res.TotalAmount += amount;
                    }

                    //  FULL ZERO GROUP SKIP
                    if (!IsGroupAllZero(res))
                        groupedResult.Add(res);
                }

                //  PAGING AFTER GROUPING
                var totalCount = groupedResult.Count;

                var pagedItems = groupedResult
                    .Skip(request.Skip)
                    .Take(request.PageSize)
                    .ToList();

                var pagedResult = new PagedResult<GroupWiseBillingResponse>(
                    pagedItems,
                    totalCount,
                    request.Skip,
                    request.PageSize);

                return ReturnData<PagedResult<GroupWiseBillingResponse>>.SuccessResponse(pagedResult, "Group wise billing retrieved", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<GroupWiseBillingResponse>>.ErrorResponse($"Error: {ex.Message}", 500);
            }
        }
        private static void FillRetailSHNI(IPO_BuyerOrder order, GroupWiseBillingResponse res, int qty, decimal amount)
        {
            var target = order.InvestorType switch
            {
                (int)IPOInvestorType.Retail => res.Retail,
                (int)IPOInvestorType.SHNI => res.SHNI,
                _ => res.BHNI
            }; target.Count += qty;
            target.Billing += amount;
        }

        private static void FillSubjectTo(IPO_BuyerOrder order, GroupWiseBillingResponse res, int qty, decimal amount)
        {
            var target = order.InvestorType switch
            {
                (int)IPOInvestorType.Retail => res.SubjectTo_Retail,
                (int)IPOInvestorType.SHNI => res.SubjectTo_SHNI,
                _ => res.SubjectTo_BHNI
            };

            target.Count += qty;
            target.Billing += amount;
        }
        private static bool IsGroupAllZero(GroupWiseBillingResponse r)
        {
            return
                r.Retail.Count == 0 &&
                r.Retail.Billing == 0 &&

                r.SHNI.Count == 0 &&
                r.SHNI.Billing == 0 &&

                r.BHNI.Count == 0 &&
                r.BHNI.Billing == 0 &&

                r.SubjectTo_Retail.Count == 0 &&
                r.SubjectTo_Retail.Billing == 0 &&

                r.SubjectTo_SHNI.Count == 0 &&
                r.SubjectTo_SHNI.Billing == 0 &&

                r.SubjectTo_BHNI.Count == 0 &&
                r.SubjectTo_BHNI.Billing == 0 &&

                r.Premium.Shares == 0 &&
                r.Premium.Billing == 0 &&

                r.Options.CallAmount == 0 &&
                r.Options.PutAmount == 0 &&

                r.TotalShares == 0 &&
                r.TotalAmount == 0;
        }
        public async Task<ReturnData<PagedResult<BuyerOrderResponse>>> GetOrderDetailPagedListByOrderIdAsync(OrderDetailFilterRequest request, int companyId, int ipoId, int orderType,int orderId)
        {
            try
            {
                var pagedResult = await _buyerPlaceOrderRepository.GetOrderDetailPagedListByOrderIdAsync(request, companyId, ipoId, orderType, orderId);

                var responses = pagedResult.Items?
                    .Select((order, index) => MapToOrderDetailResponse(
                        order,
                        srNo: request.Skip + index + 1
                    ))
                    .ToList() ?? new List<BuyerOrderResponse>();

                var result = new PagedResult<BuyerOrderResponse>(responses, pagedResult.TotalCount, request.Skip, request.PageSize)
                {
                    Extras = pagedResult.Extras // Pass through extras like totalApplications, pendingPanApplications
                };
                return ReturnData<PagedResult<BuyerOrderResponse>>.SuccessResponse(result, "Order details retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<BuyerOrderResponse>>.ErrorResponse($"Error retrieving order details: {ex.Message}", 500);
            }
        }
    }
}
