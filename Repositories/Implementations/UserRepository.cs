using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class UserRepository : BaseRepository<IPO_UserMaster>, IUserRepository
    {
        public UserRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<IPO_UserMaster?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<PagedResult<IPO_UserMaster>> GetUsersWithFiltersAsync(
            string? searchValue,
            int skip,
            int pageSize)
        {
            var query = _dbSet.AsQueryable();

            // Apply global search if provided
            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(searchValue)) ||
                    (u.FName != null && u.FName.Contains(searchValue)) ||
                    (u.LName != null && u.LName.Contains(searchValue)) ||
                    (u.Phone != null && u.Phone.Contains(searchValue))
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply offset-based pagination
            var users = await query
                .OrderBy(u => u.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<IPO_UserMaster>(users, totalCount, skip, pageSize);
        }
    }
}
