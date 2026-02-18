using IPOClient.Models.Requests.Profile;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ReturnData<ProfileResponse>> GetProfileAsync(int userId);
        Task<ReturnData> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    }
}
