using IPOClient.Models.Entities;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<IPO_UserMaster>
    {
        Task<IPO_UserMaster?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<PagedResult<IPO_UserMaster>> GetUsersWithFiltersAsync(
            string? searchValue,
            int skip,
            int pageSize);
    }
}
