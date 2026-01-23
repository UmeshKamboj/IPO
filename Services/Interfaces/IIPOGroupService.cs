using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOGroupService
    {
        Task<ReturnData> UpdateGroupAsync(int id, CreateIPOGroupRequest request, int modifiedByUserId);
        Task<ReturnData> DeleteGroupAsync(int id, int modifiedByUserId);
        Task<ReturnData<List<IPOGroupResponse>>> GetGroupsByCompanyAsync(int companyId, int? ipoId);
    }
}
