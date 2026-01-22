using IPOClient.Models.Entities;
using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;
using IPOClient.Utilities;

namespace IPOClient.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ReturnData<PagedResult<UserResponse>>> GetAllUsersAsync(UserFilterRequest request)
        {
            try
            {
                var pagedUsers = await _userRepository.GetUsersWithFiltersAsync(
                    request.SearchValue,
                    request.Skip,
                    request.PageSize);

                var userResponses = pagedUsers.Items!.Select(u => MapToUserResponse(u)).ToList();
                var result = new PagedResult<UserResponse>(userResponses, pagedUsers.TotalCount, pagedUsers.Skip, pagedUsers.PageSize);

                return ReturnData<PagedResult<UserResponse>>.SuccessResponse(result, "Users retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<UserResponse>>.ErrorResponse($"Error retrieving users: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<UserResponse>> GetUserByIdAsync(int id, int currentUserId, bool isAdmin)
        {
            try
            {
                if (!isAdmin && currentUserId != id)
                    return ReturnData<UserResponse>.ErrorResponse("You can only view your own profile", 403);

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return ReturnData<UserResponse>.ErrorResponse("User not found", 404);

                return ReturnData<UserResponse>.SuccessResponse(MapToUserResponse(user), "User retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<UserResponse>.ErrorResponse($"Error retrieving user: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<UserResponse>> CreateUserAsync(CreateUserRequest request, int createdByUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                    return ReturnData<UserResponse>.ErrorResponse("Email and password are required", 400);

                if (await _userRepository.EmailExistsAsync(request.Email))
                    return ReturnData<UserResponse>.ErrorResponse("Email already exists", 400);

                var user = new IPO_UserMaster
                {
                    FName = request.FName,
                    LName = request.LName,
                    Email = request.Email,
                    Password = PasswordHelper.HashPassword(request.Password),
                    Phone = request.Phone,
                    IsAdmin = request.IsAdmin,
                    CreatedBy = createdByUserId,
                    CreatedDate = DateTime.UtcNow,
                    ExpiryDate = request.ExpiryDate
                };

                var createdUser = await _userRepository.AddAsync(user);
                return ReturnData<UserResponse>.SuccessResponse(MapToUserResponse(createdUser), "User created successfully", 201, createdUser.Id);
            }
            catch (Exception ex)
            {
                return ReturnData<UserResponse>.ErrorResponse($"Error creating user: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> UpdateUserAsync(int id, UpdateUserRequest request, int currentUserId, bool isAdmin)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return ReturnData.ErrorResponse("User not found", 404);

                if (!isAdmin && currentUserId != id)
                {
                    // Regular users can only update their phone
                    user.Phone = request.Phone;
                }
                else
                {
                    // Admins can update all fields
                    if (!string.IsNullOrWhiteSpace(request.FName))
                        user.FName = request.FName;
                    if (!string.IsNullOrWhiteSpace(request.LName))
                        user.LName = request.LName;
                    if (!string.IsNullOrWhiteSpace(request.Phone))
                        user.Phone = request.Phone;
                    if (request.IsAdmin.HasValue)
                        user.IsAdmin = request.IsAdmin.Value;
                    if (request.ExpiryDate.HasValue)
                        user.ExpiryDate = request.ExpiryDate;
                }

                await _userRepository.UpdateAsync(user);
                return ReturnData.SuccessResponse("User updated successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error updating user: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return ReturnData.ErrorResponse("User not found", 404);

                await _userRepository.DeleteAsync(user);
                return ReturnData.SuccessResponse("User deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData.ErrorResponse($"Error deleting user: {ex.Message}", 500);
            }
        }

        private UserResponse MapToUserResponse(IPO_UserMaster user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FName = user.FName,
                LName = user.LName,
                Email = user.Email,
                Phone = user.Phone,
                IsAdmin = user.IsAdmin,
                CreatedBy = user.CreatedBy,
                CreatedDate = user.CreatedDate,
                ExpiryDate = user.ExpiryDate
            };
        }
    }
}
