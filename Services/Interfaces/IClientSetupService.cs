using IPOClient.Models.Requests.ClientSetup;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IClientSetupService
    {
        Task<ReturnData<ClientSetupResponse>> CreateClientAsync(CreateClientSetupRequest request, int userId, int companyId);
        Task<ReturnData<ClientSetupResponse>> UpdateClientAsync(UpdateClientSetupRequest request, int userId);
        Task<ReturnData> DeleteClientAsync(int id, int userId, int companyId);
        Task<ReturnData<ClientSetupResponse>> GetClientByIdAsync(int id, int companyId, bool includeDeleted = false);
        Task<ReturnData<PagedResult<ClientSetupResponse>>> GetClientsAsync(ClientSetupFilterRequest request, int companyId);
        Task<ReturnData<DeleteAllClientsResponse>> DeleteAllClientsAsync(DeleteAllClientsRequest request, int userId, int companyId);
        Task<ReturnData<PagedResult<ClientDeleteHistoryResponse>>> GetDeleteHistoryAsync(ClientDeleteHistoryFilterRequest request, int companyId);
        Task<ReturnData<ClientDeleteHistoryResponse>> GetDeleteHistoryByIdAsync(int historyId, int companyId);
    }
}
