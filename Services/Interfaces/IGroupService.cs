using IPOClient.Models.Requests.Group;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IGroupService
    {
        Task<ReturnData<GroupResponse>> CreateGroupAsync(CreateGroupRequest request, int userId, int companyId);
        Task<ReturnData<GroupResponse>> UpdateGroupAsync(UpdateGroupRequest request, int userId);
        Task<ReturnData> DeleteGroupAsync(int id, int userId, int companyId);
        Task<ReturnData<GroupResponse>> GetGroupByIdAsync(int id, int companyId);
        Task<ReturnData<PagedResult<GroupResponse>>> GetGroupsAsync(GroupFilterRequest request, int companyId);
        Task<ReturnData<List<GroupListResponse>>> GetGroupListAsync(int companyId);
    }
}
