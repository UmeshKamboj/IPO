using Azure;
using ClosedXML.Excel;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Implementations;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
                        GroupId = groupId,
                        GroupName = group?.GroupName ?? "-",
                        OrderType = order.OrderType,
                        OrderCategory = order.OrderCategory,
                        InvestorType = order.InvestorType,
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
                var result = await _buyerPlaceOrderRepository.UpdateOrderDetailsAsync(request, modifiedByUserId);
                if (result.code==-1)
                    return ReturnData.ErrorResponse(result.message, 404);

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

        public async Task<ReturnData> BulkOrderUploadAsync(int ipoId, IFormFile file, int createdByUserId, int companyId,int? orderId)
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

                var success = await _buyerPlaceOrderRepository.BulkOrderUploadAsync(ipoId, rows, createdByUserId, companyId, orderId);
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
        public async Task<ReturnData<ClientWiseBillingResponse>> GetClientWiseBillingPagedListAsync(OrderDetailFilterRequest request, int companyId, int ipoId)
        {
            try
            {
                // Get IPO master for summary data
                var ipoMaster = await _ipoRepository.GetByIdAsync(ipoId, companyId);

                // Get total billing for all filtered items (not just current page)
                var total = await _buyerPlaceOrderRepository.GetClientWiseBillingTotalAsync(request, companyId, ipoId);

                var pagedResult = await _buyerPlaceOrderRepository.GetClientWisePagedListAsync(request, companyId, ipoId);

                // Pass ipoMaster data to mapping function for accurate Amount calculation
                var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
                var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

                var responses = pagedResult.Items?
                    .Select((order, index) => MapToOrderDetailResponse(
                        order,
                        srNo: request.Skip + index + 1,
                        ipoPrice: ipoPrice,
                        ipoPreOpenPrice: ipoPreOpenPrice
                    ))
                    .ToList() ?? new List<BuyerOrderResponse>();

                var pagedData = new PagedResult<BuyerOrderResponse>(responses, pagedResult.TotalCount, request.Skip, request.PageSize);

                var result = new ClientWiseBillingResponse
                {
                    Total = total,
                    IPOPrice = ipoPrice,
                    PreOpenPrice = ipoPreOpenPrice,
                    Data = pagedData
                };

                return ReturnData<ClientWiseBillingResponse>.SuccessResponse(result, "Client wise billing retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<ClientWiseBillingResponse>.ErrorResponse($"Error retrieving order details: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> UpdatePreOpenPriceAsync(UpdatePreOpenPriceRequest request, int companyId, int userId)
        {
            try
            {
                // Priority: POChildId > OrderId > IPOId (all orders)
                if (request.POChildId.HasValue&& request.POChildId.Value>0)
                {
                    // Update single child order's PreOpenPrice
                    var updated = await _buyerPlaceOrderRepository.UpdateChildPreOpenPriceAsync(request.POChildId.Value, request.PreOpenPrice, companyId, userId);
                    if (!updated)
                        return ReturnData.ErrorResponse("Child order not found or update failed", 404);

                    return ReturnData.SuccessResponse("PreOpen Price updated for selected item", 200);
                }
                //else if (request.OrderId.HasValue && request.OrderId.Value > 0)
                //{
                //    // Update all children of a specific order
                //    var count = await _buyerPlaceOrderRepository.UpdateOrderChildrenPreOpenPriceAsync(request.OrderId.Value, request.PreOpenPrice, companyId, userId);
                //    if (count == 0)
                //        return ReturnData.ErrorResponse("Order not found or no children to update", 404);

                //    return ReturnData.SuccessResponse($"PreOpen Price updated for {count} children of order", 200);
                //}
                else
                {
                    // Update IPO Master PreOpen Price AND all child orders
                    var ipoUpdated = await _ipoRepository.UpdatePreOpenPriceAsync(request.IPOId??0, request.PreOpenPrice, companyId, userId);
                    if (!ipoUpdated)
                        return ReturnData.ErrorResponse("IPO not found or update failed", 404);

                    // Also update all child orders for this IPO
                    var count = await _buyerPlaceOrderRepository.UpdateAllChildrenPreOpenPriceAsync(request.IPOId??0, request.PreOpenPrice, companyId, userId);

                    return ReturnData.SuccessResponse($"PreOpen Price updated for all {count} orders", 200);
                }
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error updating PreOpen Price: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> SyncChildrenPreOpenPriceFromParentAsync(int ipoId, int companyId, int userId)
        {
            try
            {
                // Get IPO's PreOpenPrice
                var ipoMaster = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                if (ipoMaster == null)
                    return ReturnData.ErrorResponse("IPO not found", 404);

                var preOpenPrice = ipoMaster.OpenIPOPrice ?? 0;

                // Update all child orders that have PreOpenPrice = 0
                var count = await _buyerPlaceOrderRepository.SyncChildrenPreOpenPriceFromParentAsync(ipoId, preOpenPrice, companyId, userId);

                return ReturnData.SuccessResponse($"Synced PreOpenPrice ({preOpenPrice}) for {count} child orders", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error syncing PreOpen Price: {ex.Message}", 500);
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

        private BuyerOrderResponse MapToOrderDetailResponse(IPO_PlaceOrderChild child, int srNo, decimal? ipoPrice = null, decimal? ipoPreOpenPrice = null)
        {
            var order = child.IPOOrder;
            var master = order.BuyerMaster;

            // Get PreOpenPrice: Use child's PreOpenPrice, fallback to parent's (passed in) if child has 0
            var preOpenPrice = child.PreOpenPrice > 0 ? child.PreOpenPrice : (ipoPreOpenPrice ?? child.Group?.IPOMaster?.OpenIPOPrice ?? 0);
            // Get IPOPrice (Upper Price Band) - use passed in value or fallback to Group.IPOMaster
            var ipoPriceFinal = ipoPrice ?? child.Group?.IPOMaster?.IPO_Upper_Price_Band ?? 0;
            // Get Rate from order
            var rate = order.Rate;
            // Get AllotedQty
            var allotedQty = child.AllotedQty ?? 0;

            // New Formula: Amount = (PreOpenPrice - IPOPrice) × AllotedQty - Rate
            var amount = (preOpenPrice - ipoPriceFinal) * allotedQty - rate;

            return new BuyerOrderResponse
            {
                SrNo = srNo,
                POChildId = child.POChildId,
                OrderId = order.OrderId,
                BuyerMasterId = master.BuyerMasterId,

                GroupName = child.Group?.GroupName,
                GroupId = child.GroupId,

                OrderType = order.OrderType,
                OrderCategory = order.OrderCategory,
                InvestorType = order.InvestorType,

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
                AllotedQty = allotedQty,
                DematNumber = child.DematNumber ?? "",
                ApplicationNumber = child.ApplicationNo ?? "",
                Remark = order.Remarks,
                PreOpenPrice = preOpenPrice,
                Amount = amount
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
                GroupId = firstChild?.GroupId ?? 0,
                GroupName = firstChild?.Group?.GroupName,
                OrderType = order.OrderType,
                OrderCategory = order.OrderCategory,
                InvestorType = order.InvestorType,
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
        public async Task<ReturnData<GroupWiseBillingPagedResponse>> GetGroupWiseBillingListAsync(GroupWiseBillingRequest request, int companyId, int ipoId)
        {
            try
            {
                // Get IPO master for pricing data
                var ipoMaster = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
                var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

                var data = await _buyerPlaceOrderRepository.GetGroupWiseBillingListAsync(request, companyId, ipoId);
                var groupedResult = new List<GroupWiseBillingResponse>();

                foreach (var grp in data.GroupBy(x => x.GroupId))
                {
                    var first = grp.First();

                    var res = new GroupWiseBillingResponse
                    {
                        GroupId = first.GroupId,
                        GroupName = first.Group?.GroupName ?? "-",
                        TallyStatus = first.Group?.TallyStatus ?? false
                    };

                    foreach (var row in grp)
                    {
                        var order = row.IPOOrder;

                        // Get AllotedQty (use 0 if null)
                        var allotedQty = row.AllotedQty ?? 0;

                        // Get PreOpenPrice: Use child's PreOpenPrice, fallback to IPO's if child has 0
                        var preOpenPrice = row.PreOpenPrice > 0 ? row.PreOpenPrice : ipoPreOpenPrice;

                        // Get Rate from order
                        var rate = order.Rate;

                        // New Formula: Amount = (PreOpenPrice - IPOPrice) × AllotedQty - Rate
                        // For SELL orders, negate the amount
                        var amount = (preOpenPrice - ipoPrice) * allotedQty - rate;
                        if (order.OrderType == (int)IPOOrderType.SELL)
                            amount = -amount;

                        // Count = number of child rows (+1 for BUY, -1 for SELL)
                        var countDelta = order.OrderType == (int)IPOOrderType.BUY ? 1 : -1;

                        // Alloted = sum of AllotedQty (positive for BUY, negative for SELL)
                        var allotedDelta = order.OrderType == (int)IPOOrderType.BUY ? allotedQty : -allotedQty;

                        // ===== KOSTAK
                        if (order.OrderCategory == (int)IPOOrderCategory.Kostak)
                            FillRetailSHNI(order, res, countDelta, allotedDelta, amount);

                        // ===== SUBJECT TO
                        if (order.OrderCategory == (int)IPOOrderCategory.SubjectTo)
                            FillSubjectTo(order, res, countDelta, allotedDelta, amount);

                        // ===== PREMIUM
                        if (order.OrderCategory == (int)IPOOrderCategory.Premium)
                        {
                            res.Premium.Shares += allotedDelta;
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

                        res.TotalShares += allotedDelta;
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

                // Calculate AllTallyStatusTrue: true if all groups have TallyStatus = true
                var allTallyStatusTrue = groupedResult.Count > 0 && groupedResult.All(x => x.TallyStatus);

                var response = new GroupWiseBillingPagedResponse
                {
                    PagedResult = pagedResult,
                    AllTallyStatusTrue = allTallyStatusTrue
                };

                return ReturnData<GroupWiseBillingPagedResponse>.SuccessResponse(response, "Group wise billing retrieved", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<GroupWiseBillingPagedResponse>.ErrorResponse($"Error: {ex.Message}", 500);
            }
        }
        private static void FillRetailSHNI(IPO_BuyerOrder order, GroupWiseBillingResponse res, int countDelta, int allotedDelta, decimal amount)
        {
            var target = order.InvestorType switch
            {
                (int)IPOInvestorType.Retail => res.Retail,
                (int)IPOInvestorType.SHNI => res.SHNI,
                _ => res.BHNI
            };
            target.Count += countDelta;
            target.Alloted += allotedDelta;
            target.Billing += amount;
        }

        private static void FillSubjectTo(IPO_BuyerOrder order, GroupWiseBillingResponse res, int countDelta, int allotedDelta, decimal amount)
        {
            var target = order.InvestorType switch
            {
                (int)IPOInvestorType.Retail => res.SubjectTo_Retail,
                (int)IPOInvestorType.SHNI => res.SubjectTo_SHNI,
                _ => res.SubjectTo_BHNI
            };

            target.Count += countDelta;
            target.Alloted += allotedDelta;
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

        #region Download Methods

        public async Task<ReturnData<FileResponse>> DownloadGroupWiseBillingExcelAsync(GroupWiseBillingRequest request, int companyId, int ipoId)
        {
            try
            {
                // Get IPO master for pricing and name
                var ipoMaster = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
                var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

                var data = await _buyerPlaceOrderRepository.GetGroupWiseBillingListAsync(request, companyId, ipoId);
                var groupedResult = new List<GroupWiseBillingResponse>();

                foreach (var grp in data.GroupBy(x => x.GroupId))
                {
                    var first = grp.First();
                    var res = new GroupWiseBillingResponse
                    {
                        GroupName = first.Group?.GroupName ?? "-",
                        TallyStatus = first.Group?.TallyStatus ?? false
                    };

                    foreach (var row in grp)
                    {
                        var order = row.IPOOrder;
                        var allotedQty = row.AllotedQty ?? 0;
                        var preOpenPrice = row.PreOpenPrice > 0 ? row.PreOpenPrice : ipoPreOpenPrice;
                        var rate = order.Rate;
                        var amount = (preOpenPrice - ipoPrice) * allotedQty - rate;
                        if (order.OrderType == (int)IPOOrderType.SELL)
                            amount = -amount;

                        var countDelta = order.OrderType == (int)IPOOrderType.BUY ? 1 : -1;
                        var allotedDelta = order.OrderType == (int)IPOOrderType.BUY ? allotedQty : -allotedQty;

                        if (order.OrderCategory == (int)IPOOrderCategory.Kostak)
                            FillRetailSHNI(order, res, countDelta, allotedDelta, amount);
                        if (order.OrderCategory == (int)IPOOrderCategory.SubjectTo)
                            FillSubjectTo(order, res, countDelta, allotedDelta, amount);
                        if (order.OrderCategory == (int)IPOOrderCategory.Premium)
                        {
                            res.Premium.Shares += (int)allotedDelta;
                            res.Premium.Billing += amount;
                        }
                        if (!string.IsNullOrEmpty(order.PremiumStrikePrice) &&
                            order.PremiumStrikePrice != "Application" &&
                            order.PremiumStrikePrice != "Premium")
                        {
                            if (order.OrderType == (int)IPOOrderType.BUY)
                                res.Options.CallAmount += amount;
                            else
                                res.Options.PutAmount += amount;
                        }
                        res.TotalShares += (int)allotedDelta;
                        res.TotalAmount += amount;
                    }

                    if (!IsGroupAllZero(res))
                        groupedResult.Add(res);
                }

                // Create Excel
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Group Wise Billing");

                // Headers
                var headers = new[] { "Tally", "Group Name",
                    "Retail Count", "Retail Alloted", "Retail Billing",
                    "SHNI Count", "SHNI Alloted", "SHNI Billing",
                    "BHNI Count", "BHNI Alloted", "BHNI Billing",
                    "SubjectTo Retail Count", "SubjectTo Retail Alloted", "SubjectTo Retail Billing",
                    "SubjectTo SHNI Count", "SubjectTo SHNI Alloted", "SubjectTo SHNI Billing",
                    "SubjectTo BHNI Count", "SubjectTo BHNI Alloted", "SubjectTo BHNI Billing",
                    "Premium Shares", "Premium Billing",
                    "Options Call", "Options Put",
                    "Total Shares", "Total Amount" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                }

                // Data rows
                int rows = 2;
                foreach (var item in groupedResult)
                {
                    worksheet.Cell(rows, 1).Value = item.TallyStatus ? "Yes" : "No";
                    worksheet.Cell(rows, 2).Value = item.GroupName;
                    worksheet.Cell(rows, 3).Value = item.Retail.Count;
                    worksheet.Cell(rows, 4).Value = item.Retail.Alloted;
                    worksheet.Cell(rows, 5).Value = item.Retail.Billing;
                    worksheet.Cell(rows, 6).Value = item.SHNI.Count;
                    worksheet.Cell(rows, 7).Value = item.SHNI.Alloted;
                    worksheet.Cell(rows, 8).Value = item.SHNI.Billing;
                    worksheet.Cell(rows, 9).Value = item.BHNI.Count;
                    worksheet.Cell(rows, 10).Value = item.BHNI.Alloted;
                    worksheet.Cell(rows, 11).Value = item.BHNI.Billing;
                    worksheet.Cell(rows, 12).Value = item.SubjectTo_Retail.Count;
                    worksheet.Cell(rows, 13).Value = item.SubjectTo_Retail.Alloted;
                    worksheet.Cell(rows, 14).Value = item.SubjectTo_Retail.Billing;
                    worksheet.Cell(rows, 15).Value = item.SubjectTo_SHNI.Count;
                    worksheet.Cell(rows, 16).Value = item.SubjectTo_SHNI.Alloted;
                    worksheet.Cell(rows, 17).Value = item.SubjectTo_SHNI.Billing;
                    worksheet.Cell(rows, 18).Value = item.SubjectTo_BHNI.Count;
                    worksheet.Cell(rows, 19).Value = item.SubjectTo_BHNI.Alloted;
                    worksheet.Cell(rows, 20).Value = item.SubjectTo_BHNI.Billing;
                    worksheet.Cell(rows, 21).Value = item.Premium.Shares;
                    worksheet.Cell(rows, 22).Value = item.Premium.Billing;
                    worksheet.Cell(rows, 23).Value = item.Options.CallAmount;
                    worksheet.Cell(rows, 24).Value = item.Options.PutAmount;
                    worksheet.Cell(rows, 25).Value = item.TotalShares;
                    worksheet.Cell(rows, 26).Value = item.TotalAmount;
                    rows++;
                }

                worksheet.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                ms.Position = 0;

                var fileResponse = new FileResponse
                {
                    Bytes = ms.ToArray(),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"{ipoMaster?.IPOName ?? "IPO"}-GroupWiseBilling.xlsx"
                };

                return ReturnData<FileResponse>.SuccessResponse(fileResponse, "Excel file generated", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"Error generating Excel: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<FileResponse>> DownloadClientWiseBillingExcelAsync(OrderDetailFilterRequest request, int companyId, int ipoId)
        {
            try
            {
                var ipoMaster = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
                var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

                // Get all data without pagination
                var allRequest = new OrderDetailFilterRequest
                {
                    SearchValue = request.SearchValue,
                    GroupId = request.GroupId,
                    OrderCategoryId = request.OrderCategoryId,
                    InvestorTypeId = request.InvestorTypeId,
                    Skip = 0,
                    PageSize = int.MaxValue
                };

                var pagedResult = await _buyerPlaceOrderRepository.GetClientWisePagedListAsync(allRequest, companyId, ipoId);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Client Wise Billing");

                // Headers
                var headers = new[] { "Sr.No", "Group Name", "Order Type", "Category", "Investor Type",
                    "PAN Number", "Client Name", "Alloted Qty", "Demat Number", "Application No",
                    "Rate", "PreOpen Price", "Amount", "Remark" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                }

                // Data rows
                int row = 2;
                int srNo = 1;
                foreach (var child in pagedResult.Items ?? new List<IPO_PlaceOrderChild>())
                {
                    var order = child.IPOOrder;
                    var preOpenPrice = child.PreOpenPrice > 0 ? child.PreOpenPrice : ipoPreOpenPrice;
                    var allotedQty = child.AllotedQty ?? 0;
                    var amount = (preOpenPrice - ipoPrice) * allotedQty - order.Rate;

                    worksheet.Cell(row, 1).Value = srNo++;
                    worksheet.Cell(row, 2).Value = child.Group?.GroupName ?? "-";
                    worksheet.Cell(row, 3).Value = ((IPOOrderType)order.OrderType).ToString();
                    worksheet.Cell(row, 4).Value = ((IPOOrderCategory)order.OrderCategory).ToString();
                    worksheet.Cell(row, 5).Value = ((IPOInvestorType)order.InvestorType).ToString();
                    worksheet.Cell(row, 6).Value = child.PANNumber ?? "";
                    worksheet.Cell(row, 7).Value = child.ClientName ?? "";
                    worksheet.Cell(row, 8).Value = allotedQty;
                    worksheet.Cell(row, 9).Value = child.DematNumber ?? "";
                    worksheet.Cell(row, 10).Value = child.ApplicationNo ?? "";
                    worksheet.Cell(row, 11).Value = order.Rate;
                    worksheet.Cell(row, 12).Value = preOpenPrice;
                    worksheet.Cell(row, 13).Value = amount;
                    worksheet.Cell(row, 14).Value = order.Remarks ?? "";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                ms.Position = 0;

                var fileResponse = new FileResponse
                {
                    Bytes = ms.ToArray(),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"{ipoMaster?.IPOName ?? "IPO"}-ClientWiseBilling.xlsx"
                };

                return ReturnData<FileResponse>.SuccessResponse(fileResponse, "Excel file generated", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"Error generating Excel: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<FileResponse>> DownloadClientWiseBillingPdfAsync(OrderDetailFilterRequest request, int companyId, int ipoId)
        {
            try
            {
                var ipoMaster = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
                var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

                // Get all data without pagination
                var allRequest = new OrderDetailFilterRequest
                {
                    SearchValue = request.SearchValue,
                    GroupId = request.GroupId,
                    OrderCategoryId = request.OrderCategoryId,
                    InvestorTypeId = request.InvestorTypeId,
                    Skip = 0,
                    PageSize = int.MaxValue
                };

                var pagedResult = await _buyerPlaceOrderRepository.GetClientWisePagedListAsync(allRequest, companyId, ipoId);

                using var ms = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 10, 10, 10, 10);
                var writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 7);

                document.Add(new Paragraph($"{ipoMaster?.IPOName ?? "IPO"} - Client Wise Billing", titleFont));
                document.Add(new Paragraph($"IPO Price: {ipoPrice}, PreOpen Price: {ipoPreOpenPrice}", cellFont));
                document.Add(new Paragraph(" "));

                // Table
                var table = new PdfPTable(12) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 3, 8, 5, 5, 5, 8, 10, 5, 8, 6, 6, 6 });

                // Headers
                var headers = new[] { "Sr.No", "Group", "Type", "Category", "Investor", "PAN", "Client Name", "Alloted", "Demat", "Rate", "PreOpen", "Amount" };
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = new BaseColor(200, 220, 255),
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 3
                    };
                    table.AddCell(cell);
                }

                // Data rows
                int srNo = 1;
                foreach (var child in pagedResult.Items ?? new List<IPO_PlaceOrderChild>())
                {
                    var order = child.IPOOrder;
                    var preOpenPrice = child.PreOpenPrice > 0 ? child.PreOpenPrice : ipoPreOpenPrice;
                    var allotedQty = child.AllotedQty ?? 0;
                    var amount = (preOpenPrice - ipoPrice) * allotedQty - order.Rate;

                    table.AddCell(new PdfPCell(new Phrase(srNo++.ToString(), cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(child.Group?.GroupName ?? "-", cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(((IPOOrderType)order.OrderType).ToString(), cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(((IPOOrderCategory)order.OrderCategory).ToString(), cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(((IPOInvestorType)order.InvestorType).ToString(), cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(child.PANNumber ?? "", cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(child.ClientName ?? "", cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(allotedQty.ToString(), cellFont)) { Padding = 2, HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(child.DematNumber ?? "", cellFont)) { Padding = 2 });
                    table.AddCell(new PdfPCell(new Phrase(order.Rate.ToString("N2"), cellFont)) { Padding = 2, HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(preOpenPrice.ToString("N2"), cellFont)) { Padding = 2, HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(amount.ToString("N2"), cellFont)) { Padding = 2, HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                document.Add(table);
                document.Close();

                var fileResponse = new FileResponse
                {
                    Bytes = ms.ToArray(),
                    ContentType = "application/pdf",
                    FileName = $"{ipoMaster?.IPOName ?? "IPO"}-ClientWiseBilling.pdf"
                };

                return ReturnData<FileResponse>.SuccessResponse(fileResponse, "PDF file generated", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"Error generating PDF: {ex.Message}", 500);
            }
        }

        #endregion
    }
}
