using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class IPOGroupService : IIPOGroupService
    {
        private readonly IIPOGroupRepository _groupRepository;

        public IPOGroupService(IIPOGroupRepository groupRepository)
        {
            _groupRepository = groupRepository;
        }

        public async Task<ReturnData> UpdateGroupAsync(int id, CreateIPOGroupRequest request, int modifiedByUserId)
        {
            try
            {
                request.Id = id;
                var success = await _groupRepository.UpdateAsync(id,request, modifiedByUserId);
                if (!success)
                    return ReturnData.ErrorResponse("Group not found or inactive", 404);

                return ReturnData.SuccessResponse("Group updated successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error updating group: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> DeleteGroupAsync(int id, int modifiedByUserId)
        {
            try
            {
                var success = await _groupRepository.DeleteAsync(id, modifiedByUserId);
                if (!success)
                    return ReturnData.ErrorResponse("Group not found or already inactive", 404);

                return ReturnData.SuccessResponse("Group deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error deleting group: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<List<IPOGroupResponse>>> GetGroupsByCompanyAsync(int companyId, int? ipoId)
        {
            try
            {
                var groups = await _groupRepository.GetGroupsByCompanyAsync(companyId, ipoId);
                var dtoList = groups.Select(MapToResponse).ToList();
                return ReturnData<List<IPOGroupResponse>>.SuccessResponse(dtoList, "Groups retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<List<IPOGroupResponse>>.ErrorResponse($"Error retrieving groups: {ex.Message}", 500);
            }
        }
        private IPOGroupResponse MapToResponse(IPO_GroupMaster g)
        {
            return new IPOGroupResponse
            {
                Id = g.IPOGroupId,
                GroupName = g.GroupName,
                IsActive = g.IsActive
            };
        }
    }
}