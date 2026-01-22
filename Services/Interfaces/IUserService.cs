using IPOClient.Models.Requests;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IUserService
    {
        Task<ReturnData<PagedResult<UserResponse>>> GetAllUsersAsync(UserFilterRequest request);
        Task<ReturnData<UserResponse>> GetUserByIdAsync(int id, int currentUserId, bool isAdmin);
        Task<ReturnData<UserResponse>> CreateUserAsync(CreateUserRequest request, int createdByUserId);
        Task<ReturnData> UpdateUserAsync(int id, UpdateUserRequest request, int currentUserId, bool isAdmin);
        Task<ReturnData> DeleteUserAsync(int id);
    }
}
