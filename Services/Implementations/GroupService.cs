using IPOClient.Models.Entities;
using IPOClient.Models.Requests.Group;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _groupRepository;

        public GroupService(IGroupRepository groupRepository)
        {
            _groupRepository = groupRepository;
        }

        public async Task<ReturnData<GroupResponse>> CreateGroupAsync(CreateGroupRequest request, int userId, int companyId)
        {
            try
            {
                var groupId = await _groupRepository.CreateAsync(request, userId, companyId);
                var group = await _groupRepository.GetByIdAsync(groupId, companyId);

                if (group == null)
                    return ReturnData<GroupResponse>.ErrorResponse("Group created but could not be retrieved", 500);

                var response = MapToResponse(group);
                return ReturnData<GroupResponse>.SuccessResponse(response, "Group created successfully", 201);
            }
            catch (Exception ex)
            {
                return ReturnData<GroupResponse>.ErrorResponse($"Error creating group: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<GroupResponse>> UpdateGroupAsync(UpdateGroupRequest request, int userId)
        {
            try
            {
                var success = await _groupRepository.UpdateAsync(request, userId);
                if (!success)
                    return ReturnData<GroupResponse>.ErrorResponse("Group not found or inactive", 404);

                var group = await _groupRepository.GetByIdAsync(request.IPOGroupId, 0); // Company validation done in controller
                var response = MapToResponse(group!);
                return ReturnData<GroupResponse>.SuccessResponse(response, "Group updated successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<GroupResponse>.ErrorResponse($"Error updating group: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> DeleteGroupAsync(int id, int userId, int companyId)
        {
            try
            {
                var group = await _groupRepository.GetByIdAsync(id, companyId);
                if (group == null)
                    return ReturnData.ErrorResponse("Group not found", 404);

                var success = await _groupRepository.DeleteAsync(id, userId);
                if (!success)
                    return ReturnData.ErrorResponse("Failed to delete group", 500);

                return ReturnData.SuccessResponse("Group deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error deleting group: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<GroupResponse>> GetGroupByIdAsync(int id, int companyId)
        {
            try
            {
                var group = await _groupRepository.GetByIdAsync(id, companyId);
                if (group == null)
                    return ReturnData<GroupResponse>.ErrorResponse("Group not found", 404);

                var response = MapToResponse(group);
                return ReturnData<GroupResponse>.SuccessResponse(response, "Group retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<GroupResponse>.ErrorResponse($"Error retrieving group: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<PagedResult<GroupResponse>>> GetGroupsAsync(GroupFilterRequest request, int companyId)
        {
            try
            {
                var pagedGroups = await _groupRepository.GetGroupsWithFiltersAsync(request, companyId);
                var responses = pagedGroups.Items.Select(MapToResponse).ToList();

                var pagedResult = new PagedResult<GroupResponse>(responses, pagedGroups.TotalCount, request.Skip, request.PageSize);
                return ReturnData<PagedResult<GroupResponse>>.SuccessResponse(pagedResult, "Groups retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<GroupResponse>>.ErrorResponse($"Error retrieving groups: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<List<GroupListResponse>>> GetGroupListAsync(int companyId)
        {
            try
            {
                var groups = await _groupRepository.GetAllByCompanyAsync(companyId);
                var responses = groups.Select(g => new GroupListResponse
                {
                    IPOGroupId = g.IPOGroupId,
                    GroupName = g.GroupName ?? string.Empty
                }).ToList();
                return ReturnData<List<GroupListResponse>>.SuccessResponse(responses, "Groups retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<List<GroupListResponse>>.ErrorResponse($"Error retrieving groups: {ex.Message}", 500);
            }
        }

        private GroupResponse MapToResponse(IPO_GroupMaster group)
        {
            return new GroupResponse
            {
                IPOGroupId = group.IPOGroupId,
                GroupName = group.GroupName,
                MobileNo = group.MobileNo,
                Email = group.Email,
                Address = group.Address,
                Remark = group.Remark,
                IPOId = group.IPOId,
                IPOName = group.IPOMaster?.IPOName,
                CompanyId = group.CompanyId,
                CreatedDate = group.CreatedDate,
                CreatedBy = group.CreatedBy,
                ModifiedDate = group.ModifiedDate,
                ModifiedBy = group.ModifiedBy
            };
        }
    }
}
