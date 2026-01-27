using IPOClient.Models.Entities;
using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Implementations;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class IPOOrderRemarkService: IIPOOrderRemarkService
    {
        private IIPOOrderRemarkRepository _iPOOrderRemarkRepo;
        public IPOOrderRemarkService(IIPOOrderRemarkRepository iPOOrderRemarkRepo)
        {
            _iPOOrderRemarkRepo = iPOOrderRemarkRepo;
        }

        public async Task<ReturnData<OrderRemarkResponse>> CreateOrderRemarkAsync(CreateOrderRemarkRequest request, int userId, int companyId)
        {
            try
            {
                var remarkId = await _iPOOrderRemarkRepo.CreateAsync(request, userId, companyId);
                var remark = await _iPOOrderRemarkRepo.GetByIdAsync(remarkId, companyId);

                if (remark == null)
                    return ReturnData<OrderRemarkResponse>.ErrorResponse("Remark created but could not be retrieved", 500);

                var response = MapToResponse(remark);
                return ReturnData<OrderRemarkResponse>.SuccessResponse(response, "Remark created successfully", 201);
            }
            catch (Exception ex)
            {
                return ReturnData<OrderRemarkResponse>.ErrorResponse($"Error creating remark: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<List<OrderRemarkDTOResponse>>> GetOrderRemarkDTOByCompanyAsync(int companyId, int? ipoId)
        {
            try
            {
                var remarks = await _iPOOrderRemarkRepo.GetRemarkByCompanyAsync(companyId, ipoId);
                var dtoList = remarks.Select(r => new OrderRemarkDTOResponse
                {
                    RemarkId = r.RemarkId,
                    Remark = r.Remark
                }).ToList();
                return ReturnData<List<OrderRemarkDTOResponse>>.SuccessResponse(dtoList, "Remarks retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<List<OrderRemarkDTOResponse>>.ErrorResponse($"Error retrieving remarks: {ex.Message}", 500);
            }
        }
        private OrderRemarkResponse MapToResponse(IPO_Order_Remark g)
        {
            return new OrderRemarkResponse
            {
                RemarkId = g.RemarkId,
                Remark = g.Remark,
                IPOId = g.IPOId,
                CreatedDate= g.CreatedDate,
                IsActive= g.IsActive
            };
        }
    }
}
