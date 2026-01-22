using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class IPOService : IIPOService
    {
        private readonly IIPORepository _ipoRepository;

        public IPOService(IIPORepository ipoRepository)
        {
            _ipoRepository = ipoRepository;
        }

        // GET ALL IPOs with filters and paging
        public async Task<ReturnData<PagedResult<CreateIPOResponse>>> GetAllIPOsAsync(IPOFilterRequest request, int companyId)
        {
            try
            {
                var pagedIPOs = await _ipoRepository.GetIPOsWithFiltersAsync(
                    request, companyId
                );
                var ipoResponses = pagedIPOs.Items!.Select(i => MapToIPOResponse(i)).ToList();
                var result = new PagedResult<CreateIPOResponse>(ipoResponses, pagedIPOs.TotalCount, pagedIPOs.Skip, pagedIPOs.PageSize); 
                return ReturnData<PagedResult<CreateIPOResponse>>.SuccessResponse(result, "IPOs retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<CreateIPOResponse>>.ErrorResponse($"Error retrieving IPOs: {ex.Message}", 500);
            }
        }

        // GET BY ID
        public async Task<ReturnData<CreateIPOResponse>> GetIPOByIdAsync(int id, int companyId)
        {
            try
            {
                var ipo = await _ipoRepository.GetByIdAsync(id, companyId);
                if (ipo == null)
                    return ReturnData<CreateIPOResponse>.ErrorResponse("IPO not found", 404);

                return ReturnData<CreateIPOResponse>.SuccessResponse(MapToIPOResponse(ipo), "IPO retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<CreateIPOResponse>.ErrorResponse($"Error retrieving IPO: {ex.Message}", 500);
            }
        }

        // CREATE IPO
        public async Task<ReturnData<CreateIPOResponse>> CreateIPOAsync(CreateIPORequest request, int createdByUserId, int companyId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IPOName))
                    return ReturnData<CreateIPOResponse>.ErrorResponse("IPO Name is required", 400);
               
                if (!Enum.IsDefined(typeof(IPOType), request.IPOType))
                {
                    return ReturnData<CreateIPOResponse>.ErrorResponse($"Invalid IPOType: {request.IPOType}", 400); 
                } 
                var ipoId = await _ipoRepository.CreateAsync(request, createdByUserId, companyId);
                var createdIPO = await _ipoRepository.GetByIdAsync(ipoId, companyId);

                return ReturnData<CreateIPOResponse>.SuccessResponse(MapToIPOResponse(createdIPO!), "IPO created successfully", 201);
            }
            catch (Exception ex)
            {
                return ReturnData<CreateIPOResponse>.ErrorResponse($"Error creating IPO: {ex.Message}", 500);
            }
        }

        // UPDATE IPO
        public async Task<ReturnData> UpdateIPOAsync(int id, CreateIPORequest request, int modifiedByUserId)
        {
            try
            {
                request.Id = id; 
                // Validate IPOType
                if (!Enum.IsDefined(typeof(IPOType), request.IPOType))
                {
                    return ReturnData.ErrorResponse($"Invalid IPOType: {request.IPOType}", 400);
                }
                var success = await _ipoRepository.UpdateAsync(request, modifiedByUserId);
                if (!success)
                    return ReturnData.ErrorResponse("IPO not found or inactive", 404);

                return ReturnData.SuccessResponse("IPO updated successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error updating IPO: {ex.Message}", 500);
            }
        }

        // DELETE IPO (soft delete)
        public async Task<ReturnData> DeleteIPOAsync(int id, int modifiedByUserId)
        {
            try
            {
                var success = await _ipoRepository.DeleteAsync(id, modifiedByUserId);
                if (!success)
                    return ReturnData.ErrorResponse("IPO not found or already inactive", 404);

                return ReturnData.SuccessResponse("IPO deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error deleting IPO: {ex.Message}", 500);
            }
        }
        // Get Ipo name+id list for a company
        public async Task<ReturnData<List<IPONameIdResponse>>> GetIPONameIdByCompanyAsync(int companyId)
        {
            try
            {
                var ipos = await _ipoRepository.GetIPONameIdByCompanyAsync(companyId);
                var dtoList = ipos?
                    .Select(i => new IPONameIdResponse { Id = i.Id, IPOName = i.IPOName })
                    .ToList() ?? new List<IPONameIdResponse>();

                return ReturnData<List<IPONameIdResponse>>.SuccessResponse(dtoList, "IPO names retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<List<IPONameIdResponse>>.ErrorResponse($"Error retrieving IPO names: {ex.Message}", 500);
            }
        }
        // MAP ENTITY TO RESPONSE DTO
        private CreateIPOResponse MapToIPOResponse(IPO_IPOMaster ipo)
        {
            return new CreateIPOResponse
            {
                Id = ipo.Id,
                IPOName = ipo.IPOName,
                IPOType = ipo.IPOType,
                IPO_Upper_Price_Band = ipo.IPO_Upper_Price_Band,
                Total_IPO_Size_Cr = ipo.Total_IPO_Size_Cr,
                IPO_Retail_Lot_Size = ipo.IPO_Retail_Lot_Size,
                IPO_SHNI_Lot_Size = ipo.IPO_SHNI_Lot_Size,
                IPO_BHNI_Lot_Size = ipo.IPO_BHNI_Lot_Size,
                Retail_Percentage = ipo.Retail_Percentage,
                BHNI_Percentage = ipo.BHNI_Percentage,
                SHNI_Percentage = ipo.SHNI_Percentage,
                Remark = ipo.Remark, 
                IsActive = ipo.IsActive, 
            };
        }
    }

}
