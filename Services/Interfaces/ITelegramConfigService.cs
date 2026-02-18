using IPOClient.Models.Requests.Telegram;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface ITelegramConfigService
    {
        Task<ReturnData<TelegramConfigResponse>> GetByUserIdAsync(int userId);
        Task<ReturnData<TelegramConfigResponse>> CreateOrUpdateAsync(int userId, TelegramConfigRequest request);
    }
}
