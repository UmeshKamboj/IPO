using IPOClient.Models.Requests;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ReturnData<LoginResponse>> LoginAsync(LoginRequest request);
        Task LogoutAsync();
        // TODO: Re-enable refresh token later
        // Task<ReturnData<LoginResponse>> RefreshTokenAsync();
    }
}
