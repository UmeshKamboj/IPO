using IPOClient.Models.Entities;
using IPOClient.Models.Requests.Group;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
    public interface IGroupRepository : IRepository<IPO_GroupMaster>
    {
        Task<int> CreateAsync(CreateGroupRequest request, int userId, int companyId);
        Task<bool> UpdateAsync(int id, UpdateGroupRequest request, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<IPO_GroupMaster?> GetByIdAsync(int id, int companyId);
        Task<PagedResult<IPO_GroupMaster>> GetGroupsWithFiltersAsync(GroupFilterRequest request, int companyId);
        Task<List<IPO_GroupMaster>> GetAllByCompanyAsync(int companyId);
    }
}
