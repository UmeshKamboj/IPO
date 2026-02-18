using IPOClient.Models.Requests.Email;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IEmailConfigService
    {
        Task<ReturnData<EmailConfigResponse>> GetByUserIdAsync(int userId);
        Task<ReturnData<EmailConfigResponse>> CreateOrUpdateAsync(int userId, EmailConfigRequest request);
    }
}
