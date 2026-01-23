using IPOClient.Models.Entities;
using IPOClient.Models.Requests.ClientSetup;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
    public interface IClientSetupRepository : IRepository<IPO_ClientSetup>
    {
        Task<int> CreateAsync(CreateClientSetupRequest request, int userId, int companyId);
        Task<bool> UpdateAsync(UpdateClientSetupRequest request, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<IPO_ClientSetup?> GetByIdAsync(int id, int companyId, bool includeDeleted = false);
        Task<PagedResult<IPO_ClientSetup>> GetClientsWithFiltersAsync(ClientSetupFilterRequest request, int companyId);
        Task<int> DeleteAllAsync(DeleteAllClientsRequest request, int userId, int companyId);
        Task<PagedResult<IPO_ClientDeleteHistory>> GetDeleteHistoryAsync(ClientDeleteHistoryFilterRequest request, int companyId);
        Task<IPO_ClientDeleteHistory?> GetDeleteHistoryByIdAsync(int historyId, int companyId);
    }
}
