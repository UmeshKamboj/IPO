using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
   
    public interface IIPORepository : IRepository<IPO_IPOMaster>
    {
        Task<IPO_IPOMaster?> GetByIdAsync(int id, int companyId);
        Task<int> CreateAsync(CreateIPORequest request, int userId, int companyId);
        Task<bool> UpdateAsync(CreateIPORequest request, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<PagedResult<IPO_IPOMaster>> GetIPOsWithFiltersAsync(IPOFilterRequest request, int companyId);
        Task<List<IPO_IPOMaster>> GetIPONameIdByCompanyAsync(int companyId);


       
    }
}
