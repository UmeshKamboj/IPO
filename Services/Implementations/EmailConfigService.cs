using IPOClient.Models.Entities;
using IPOClient.Models.Requests.Email;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class EmailConfigService : IEmailConfigService
    {
        private readonly IEmailConfigRepository _emailConfigRepository;

        public EmailConfigService(IEmailConfigRepository emailConfigRepository)
        {
            _emailConfigRepository = emailConfigRepository;
        }

        public async Task<ReturnData<EmailConfigResponse>> GetByUserIdAsync(int userId)
        {
            try
            {
                var config = await _emailConfigRepository.GetByUserIdAsync(userId);
                if (config == null)
                    return ReturnData<EmailConfigResponse>.SuccessResponse(null, "No email configuration found", 200);

                return ReturnData<EmailConfigResponse>.SuccessResponse(MapToResponse(config), "Email configuration retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<EmailConfigResponse>.ErrorResponse($"Error retrieving email configuration: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<EmailConfigResponse>> CreateOrUpdateAsync(int userId, EmailConfigRequest request)
        {
            try
            {
                var existing = await _emailConfigRepository.GetByUserIdAsync(userId);

                if (existing != null)
                {
                    existing.Email = request.Email;
                    existing.AppPassword = request.AppPassword;
                    existing.ModifiedDate = DateTime.UtcNow;

                    await _emailConfigRepository.UpdateAsync(existing);
                    return ReturnData<EmailConfigResponse>.SuccessResponse(MapToResponse(existing), "Email configuration updated successfully", 200);
                }

                var config = new IPO_EmailConfig
                {
                    UserId = userId,
                    Email = request.Email,
                    AppPassword = request.AppPassword,
                    CreatedDate = DateTime.UtcNow
                };

                var created = await _emailConfigRepository.AddAsync(config);
                return ReturnData<EmailConfigResponse>.SuccessResponse(MapToResponse(created), "Email configuration created successfully", 201, created.Id);
            }
            catch (Exception ex)
            {
                return ReturnData<EmailConfigResponse>.ErrorResponse($"Error saving email configuration: {ex.Message}", 500);
            }
        }

        private EmailConfigResponse MapToResponse(IPO_EmailConfig config)
        {
            return new EmailConfigResponse
            {
                Id = config.Id,
                UserId = config.UserId,
                Email = config.Email,
                AppPassword = config.AppPassword
            };
        }
    }
}
