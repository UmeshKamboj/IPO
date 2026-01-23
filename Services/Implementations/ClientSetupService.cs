using IPOClient.Models.Entities;
using IPOClient.Models.Requests.ClientSetup;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class ClientSetupService : IClientSetupService
    {
        private readonly IClientSetupRepository _clientSetupRepository;

        public ClientSetupService(IClientSetupRepository clientSetupRepository)
        {
            _clientSetupRepository = clientSetupRepository;
        }

        public async Task<ReturnData<ClientSetupResponse>> CreateClientAsync(CreateClientSetupRequest request, int userId, int companyId)
        {
            try
            {
                var clientId = await _clientSetupRepository.CreateAsync(request, userId, companyId);
                var client = await _clientSetupRepository.GetByIdAsync(clientId, companyId);

                if (client == null)
                    return ReturnData<ClientSetupResponse>.ErrorResponse("Client created but could not be retrieved", 500);

                var response = MapToResponse(client);
                return ReturnData<ClientSetupResponse>.SuccessResponse(response, "Client created successfully", 201);
            }
            catch (Exception ex)
            {
                return ReturnData<ClientSetupResponse>.ErrorResponse($"Error creating client: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<ClientSetupResponse>> UpdateClientAsync(UpdateClientSetupRequest request, int userId)
        {
            try
            {
                var success = await _clientSetupRepository.UpdateAsync(request, userId);
                if (!success)
                    return ReturnData<ClientSetupResponse>.ErrorResponse("Client not found or already deleted", 404);

                var client = await _clientSetupRepository.GetByIdAsync(request.ClientId, 0); // Company validation done in controller
                var response = MapToResponse(client!);
                return ReturnData<ClientSetupResponse>.SuccessResponse(response, "Client updated successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<ClientSetupResponse>.ErrorResponse($"Error updating client: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> DeleteClientAsync(int id, int userId, int companyId)
        {
            try
            {
                var client = await _clientSetupRepository.GetByIdAsync(id, companyId);
                if (client == null)
                    return ReturnData.ErrorResponse("Client not found", 404);

                var success = await _clientSetupRepository.DeleteAsync(id, userId);
                if (!success)
                    return ReturnData.ErrorResponse("Failed to delete client", 500);

                return ReturnData.SuccessResponse("Client deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error deleting client: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<ClientSetupResponse>> GetClientByIdAsync(int id, int companyId, bool includeDeleted = false)
        {
            try
            {
                var client = await _clientSetupRepository.GetByIdAsync(id, companyId, includeDeleted);
                if (client == null)
                    return ReturnData<ClientSetupResponse>.ErrorResponse("Client not found", 404);

                var response = MapToResponse(client);
                return ReturnData<ClientSetupResponse>.SuccessResponse(response, "Client retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<ClientSetupResponse>.ErrorResponse($"Error retrieving client: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<PagedResult<ClientSetupResponse>>> GetClientsAsync(ClientSetupFilterRequest request, int companyId)
        {
            try
            {
                var pagedClients = await _clientSetupRepository.GetClientsWithFiltersAsync(request, companyId);
                var responses = pagedClients.Items?.Select(MapToResponse).ToList() ?? new List<ClientSetupResponse>();

                var pagedResult = new PagedResult<ClientSetupResponse>(responses, pagedClients.TotalCount, request.Skip, request.PageSize);
                return ReturnData<PagedResult<ClientSetupResponse>>.SuccessResponse(pagedResult, "Clients retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<ClientSetupResponse>>.ErrorResponse($"Error retrieving clients: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<DeleteAllClientsResponse>> DeleteAllClientsAsync(DeleteAllClientsRequest request, int userId, int companyId)
        {
            try
            {
                var historyId = await _clientSetupRepository.DeleteAllAsync(request, userId, companyId);

                if (historyId == 0)
                    return ReturnData<DeleteAllClientsResponse>.ErrorResponse("No clients found to delete", 404);

                var history = await _clientSetupRepository.GetDeleteHistoryByIdAsync(historyId, companyId);

                var response = new DeleteAllClientsResponse
                {
                    HistoryId = historyId,
                    TotalClientsDeleted = history?.TotalClientsDeleted ?? 0,
                    DeletedDate = history?.DeletedDate ?? DateTime.UtcNow,
                    Message = $"Successfully deleted {history?.TotalClientsDeleted ?? 0} clients"
                };

                return ReturnData<DeleteAllClientsResponse>.SuccessResponse(response, "All clients deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<DeleteAllClientsResponse>.ErrorResponse($"Error deleting all clients: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<PagedResult<ClientDeleteHistoryResponse>>> GetDeleteHistoryAsync(ClientDeleteHistoryFilterRequest request, int companyId)
        {
            try
            {
                var pagedHistory = await _clientSetupRepository.GetDeleteHistoryAsync(request, companyId);
                var responses = pagedHistory.Items?.Select(MapToHistoryResponse).ToList() ?? new List<ClientDeleteHistoryResponse>();

                var pagedResult = new PagedResult<ClientDeleteHistoryResponse>(responses, pagedHistory.TotalCount, request.Skip, request.PageSize);
                return ReturnData<PagedResult<ClientDeleteHistoryResponse>>.SuccessResponse(pagedResult, "Delete history retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<ClientDeleteHistoryResponse>>.ErrorResponse($"Error retrieving delete history: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<ClientDeleteHistoryResponse>> GetDeleteHistoryByIdAsync(int historyId, int companyId)
        {
            try
            {
                var history = await _clientSetupRepository.GetDeleteHistoryByIdAsync(historyId, companyId);
                if (history == null)
                    return ReturnData<ClientDeleteHistoryResponse>.ErrorResponse("Delete history not found", 404);

                var response = MapToHistoryResponseWithDetails(history);
                return ReturnData<ClientDeleteHistoryResponse>.SuccessResponse(response, "Delete history retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<ClientDeleteHistoryResponse>.ErrorResponse($"Error retrieving delete history: {ex.Message}", 500);
            }
        }

        private ClientSetupResponse MapToResponse(IPO_ClientSetup client)
        {
            return new ClientSetupResponse
            {
                ClientId = client.ClientId,
                PANNumber = client.PANNumber,
                Name = client.Name,
                GroupId = client.GroupId,
                GroupName = client.Group?.GroupName,
                ClientDPId = client.ClientDPId,
                CompanyId = client.CompanyId,
                IsDeleted = client.IsDeleted,
                CreatedBy = client.CreatedBy,
                CreatedDate = client.CreatedDate,
                ModifiedBy = client.ModifiedBy,
                ModifiedDate = client.ModifiedDate,
                DeletedBy = client.DeletedBy,
                DeletedDate = client.DeletedDate
            };
        }

        private ClientDeleteHistoryResponse MapToHistoryResponse(IPO_ClientDeleteHistory history)
        {
            return new ClientDeleteHistoryResponse
            {
                HistoryId = history.HistoryId,
                DeletedDate = history.DeletedDate,
                DeletedBy = history.DeletedBy,
                CompanyId = history.CompanyId,
                TotalClientsDeleted = history.TotalClientsDeleted,
                Remark = history.Remark
            };
        }

        private ClientDeleteHistoryResponse MapToHistoryResponseWithDetails(IPO_ClientDeleteHistory history)
        {
            return new ClientDeleteHistoryResponse
            {
                HistoryId = history.HistoryId,
                DeletedDate = history.DeletedDate,
                DeletedBy = history.DeletedBy,
                CompanyId = history.CompanyId,
                TotalClientsDeleted = history.TotalClientsDeleted,
                Remark = history.Remark,
                Details = history.Details?.Select(d => new ClientDeleteHistoryDetailResponse
                {
                    DetailId = d.DetailId,
                    ClientId = d.ClientId,
                    PANNumber = d.PANNumber,
                    Name = d.Name,
                    GroupId = d.GroupId,
                    ClientDPId = d.ClientDPId
                }).ToList()
            };
        }
    }
}
