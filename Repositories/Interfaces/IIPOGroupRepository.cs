using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;

namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOGroupRepository: IRepository<IPO_GroupMaster>
    {
        Task<IPO_GroupMaster?> GetByIdAsync(int id, int companyId);
        Task<int> CreateAsync(CreateIPOGroupRequest request, int userId, int companyId);
        Task<bool> UpdateAsync(int id, CreateIPOGroupRequest request, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<List<IPO_GroupMaster>> GetGroupsByCompanyAsync(int companyId, int? ipoId);
        
    }
}
