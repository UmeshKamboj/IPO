using IPOClient.Models.Entities;
using IPOClient.Models.Requests.Telegram;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class TelegramConfigService : ITelegramConfigService
    {
        private readonly ITelegramConfigRepository _telegramConfigRepository;

        public TelegramConfigService(ITelegramConfigRepository telegramConfigRepository)
        {
            _telegramConfigRepository = telegramConfigRepository;
        }

        public async Task<ReturnData<TelegramConfigResponse>> GetByUserIdAsync(int userId)
        {
            try
            {
                var config = await _telegramConfigRepository.GetByUserIdAsync(userId);
                if (config == null)
                    return ReturnData<TelegramConfigResponse>.SuccessResponse(null, "No telegram configuration found", 200);

                return ReturnData<TelegramConfigResponse>.SuccessResponse(MapToResponse(config), "Telegram configuration retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<TelegramConfigResponse>.ErrorResponse($"Error retrieving telegram configuration: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<TelegramConfigResponse>> CreateOrUpdateAsync(int userId, TelegramConfigRequest request)
        {
            try
            {
                var existing = await _telegramConfigRepository.GetByUserIdAsync(userId);

                if (existing != null)
                {
                    existing.TelegramAPIKey = request.TelegramAPIKey;
                    existing.MobileNumber = request.MobileNumber;
                    existing.ModifiedDate = DateTime.UtcNow;

                    await _telegramConfigRepository.UpdateAsync(existing);
                    return ReturnData<TelegramConfigResponse>.SuccessResponse(MapToResponse(existing), "Telegram configuration updated successfully", 200);
                }

                var config = new IPO_TelegramConfig
                {
                    UserId = userId,
                    TelegramAPIKey = request.TelegramAPIKey,
                    MobileNumber = request.MobileNumber,
                    CreatedDate = DateTime.UtcNow
                };

                var created = await _telegramConfigRepository.AddAsync(config);
                return ReturnData<TelegramConfigResponse>.SuccessResponse(MapToResponse(created), "Telegram configuration created successfully", 201, created.Id);
            }
            catch (Exception ex)
            {
                return ReturnData<TelegramConfigResponse>.ErrorResponse($"Error saving telegram configuration: {ex.Message}", 500);
            }
        }

        private TelegramConfigResponse MapToResponse(IPO_TelegramConfig config)
        {
            return new TelegramConfigResponse
            {
                Id = config.Id,
                UserId = config.UserId,
                TelegramAPIKey = config.TelegramAPIKey,
                MobileNumber = config.MobileNumber
            };
        }
    }
}
