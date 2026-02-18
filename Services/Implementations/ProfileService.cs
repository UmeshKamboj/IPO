using IPOClient.Models.Requests.Profile;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;
using IPOClient.Utilities;

namespace IPOClient.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;

        public ProfileService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ReturnData<ProfileResponse>> GetProfileAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return ReturnData<ProfileResponse>.ErrorResponse("User not found", 404);

                var response = new ProfileResponse
                {
                    Id = user.Id,
                    FName = user.FName,
                    LName = user.LName,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsAdmin = user.IsAdmin,
                    ExpiryDate = user.ExpiryDate
                };

                return ReturnData<ProfileResponse>.SuccessResponse(response, "Profile retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<ProfileResponse>.ErrorResponse($"Error retrieving profile: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                    return ReturnData.ErrorResponse("Current password is required", 400);

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return ReturnData.ErrorResponse("New password is required", 400);

                if (request.NewPassword != request.ConfirmNewPassword)
                    return ReturnData.ErrorResponse("New password and confirm password do not match", 400);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return ReturnData.ErrorResponse("User not found", 404);

                if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.Password!))
                    return ReturnData.ErrorResponse("Current password is incorrect", 400);

                user.Password = PasswordHelper.HashPassword(request.NewPassword);
                await _userRepository.UpdateAsync(user);

                return ReturnData.SuccessResponse("Password changed successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error changing password: {ex.Message}", 500);
            }
        }
    }
}
