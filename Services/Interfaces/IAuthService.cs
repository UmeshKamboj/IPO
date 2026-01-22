using IPOClient.Models.Requests;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ReturnData<LoginResponse>> LoginAsync(LoginRequest request);
        Task LogoutAsync();
        Task<ReturnData<LoginResponse>> RefreshTokenAsync();
    }
}
